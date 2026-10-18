# Seed demo data through the live API to verify end-to-end flows.
# Requires: API running on http://localhost:8080 and Postgres container "vetclinic-postgres".
# Data profile: Peru (nombres, DNI, RUC, S/ IGV 18%, medios Yape/Plin) con volumen alto.
#
# Uso:
#   powershell -ExecutionPolicy Bypass -File .\scripts\seed-demo-api.ps1            # reset + seed
#   powershell -ExecutionPolicy Bypass -File .\scripts\seed-demo-api.ps1 -NoReset   # sin reset
#
param(
    [string]$Base = 'http://localhost:8080',
    [string]$PgContainer = 'vetclinic-postgres',
    [string]$PgUser = 'vetapp',
    [string]$PgDb = 'vetclinic',
    [switch]$NoReset,
    [switch]$NoBackup
)

$ErrorActionPreference = 'Stop'
$base = $Base
$results = @()
$script:h = @{}
$script:token = $null

function Log([string]$ok, [string]$what, $detail = '') {
    $script:results += [pscustomobject]@{ Ok = $ok; What = $what; Detail = [string]$detail }
    $mark = if ($ok -eq 'OK') { '  [OK]' } elseif ($ok -eq 'EXPECTED') { '  [EXP]' } else { ' [FAIL]' }
    Write-Host "$mark $what $detail"
}

function IsoUtc($value) {
    # PostgreSQL usa timestamp with time zone -> Npgsql exige Kind=Utc.
    return ([datetime]$value).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'")
}

function CallRequest([string]$method, [string]$path, $body = $null, $headers = $null) {
    $hdr = if ($null -eq $headers) { $script:h } else { $headers }
    try {
        $params = @{ Uri = "$base$path"; Method = $method; Headers = $hdr }
        if ($null -ne $body) {
            $json = ($body | ConvertTo-Json -Depth 10)
            $params.ContentType = 'application/json; charset=utf-8'
            $params.Body = [System.Text.Encoding]::UTF8.GetBytes($json)
        }
        $r = Invoke-RestMethod @params
        return @{ ok = $true; status = 200; data = $r }
    } catch {
        $status = 0
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) { $status = [int]$_.Exception.Response.StatusCode }
        $msg = $_.ErrorDetails.Message
        if (-not $msg) { $msg = $_.Exception.Message }
        return @{ ok = $false; status = $status; error = $msg }
    }
}
function Post([string]$path, $body) { return CallRequest 'POST' $path $body }
function GetJson([string]$path, $headers = $null) { return CallRequest 'GET' $path $null $headers }
function Patch([string]$path, $body) { return CallRequest 'PATCH' $path $body }
function Put([string]$path, $body) { return CallRequest 'PUT' $path $body }
function Delete([string]$path) { return CallRequest 'DELETE' $path $null }

function Login([string]$user, [string]$pass) {
    try {
        $r = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType 'application/json' -Body (@{ username = $user; password = $pass } | ConvertTo-Json)
        return @{ ok = $true; token = $r.token; permissions = $r.permissions; user = $r.user }
    } catch {
        $status = 0
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) { $status = [int]$_.Exception.Response.StatusCode }
        return @{ ok = $false; status = $status; error = $_.ErrorDetails.Message }
    }
}

function ExecSql([string]$sql) {
    $out = $sql | docker exec -i $PgContainer psql -U $PgUser -d $PgDb -v ON_ERROR_STOP=1 -t 2>&1
    return @{ ok = ($LASTEXITCODE -eq 0); out = ($out | Out-String).Trim() }
}

function PostFile([string]$path, [string]$filePath, [string]$contentType = 'text/plain') {
    try {
        Add-Type -AssemblyName System.Net.Http
        $client = New-Object System.Net.Http.HttpClient
        $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $script:token)
        $content = New-Object System.Net.Http.MultipartFormDataContent
        $bytes = [System.IO.File]::ReadAllBytes($filePath)
        $fileContent = New-Object System.Net.Http.ByteArrayContent(,$bytes)
        $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse($contentType)
        $content.Add($fileContent, 'file', [System.IO.Path]::GetFileName($filePath))
        $resp = $client.PostAsync("$base$path", $content).Result
        $status = [int]$resp.StatusCode
        $body = $resp.Content.ReadAsStringAsync().Result
        if ($status -ge 200 -and $status -lt 300) {
            $data = $null; if ($body) { $data = $body | ConvertFrom-Json }
            return @{ ok = $true; status = $status; data = $data }
        }
        return @{ ok = $false; status = $status; error = $body }
    } catch {
        return @{ ok = $false; status = 0; error = $_.Exception.Message }
    }
}

function TestDownload([string]$path) {
    try {
        Add-Type -AssemblyName System.Net.Http
        $client = New-Object System.Net.Http.HttpClient
        $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $script:token)
        $resp = $client.GetAsync("$base$path").Result
        $bytes = $resp.Content.ReadAsByteArrayAsync().Result
        return @{ status = [int]$resp.StatusCode; length = $bytes.Length }
    } catch {
        return @{ status = 0; length = 0 }
    }
}

# =========================================================
# F0 - RESET LIMPIO (backup + wipe de tablas operativas)
# =========================================================
if (-not $NoReset) {
    if (-not $NoBackup) {
        try {
            & (Join-Path $PSScriptRoot 'backup-db.ps1') | Out-Null
            Log 'OK' 'Backup previo antes del reset'
        } catch {
            Log 'FAIL' 'Backup previo' $_.Exception.Message
        }
    }
    $wipe = @'
TRUNCATE TABLE "Attachments","LabResults","Prescriptions","MedicalRecords","MedicationAdministrations","HospitalizationEvolutions","Hospitalizations","VaccineApplications","Reminders","InvoiceLines","Invoices","StockMovements","ProductLots","InventoryItems","Appointments","Pets","Tutors" RESTART IDENTITY CASCADE;
'@
    $r = ExecSql $wipe
    Log $(if ($r.ok) { 'OK' } else { 'FAIL' }) 'Reset de datos operativos' $(if ($r.ok) { '' } else { $r.out })
}

# =========================================================
# LOGIN
# =========================================================
$login = Login 'admin' 'Admin123!'
if (-not $login.ok) { Log 'FAIL' 'Login admin'; throw "No se pudo iniciar sesion como admin ($($login.status)) $($login.error)" }
$script:h = @{ Authorization = "Bearer $($login.token)" }
$script:token = $login.token
Log 'OK' "Login admin (permisos: $($login.permissions.Count))"

$users = (GetJson '/api/auth/users').data
$vetId = ($users | Where-Object { $_.role -eq 'Veterinario' } | Select-Object -First 1).id
$recepId = ($users | Where-Object { $_.role -eq 'Recepcionista' } | Select-Object -First 1).id
Log 'OK' "Usuarios base: vet=$vetId recepcion=$recepId"
if (-not $vetId) { throw 'No se encontró un usuario con rol Veterinario; abortando.' }

# =========================================================
# F1 - CONFIGURACION (Peru / IGV 18%)
# =========================================================
$clinic = @{
    Name = 'Clínica Veterinaria Huellitas del Perú'
    LegalName = 'Huellitas del Perú S.A.C.'
    TaxId = '20512345678'
    TaxIdType = 'RUC'
    Address = 'Av. Los Próceres 1234, Surco, Lima'
    Phone = '+51 1 4567890'
    Email = 'contacto@huellitas.pe'
    LogoDataUrl = $null
    InvoiceHeader = 'Huellitas del Perú S.A.C. - RUC 20512345678'
    InvoiceFooter = 'Gracias por su preferencia. Conserve su comprobante.'
}
$rc = Put '/api/settings/clinic' $clinic
if ($rc.ok) { Log 'OK' "Config clínica" "taxIdType=$($rc.data.taxIdType)" } else { Log 'FAIL' 'Config clínica' "$($rc.status) $($rc.error)" }

$billing = @{
    TaxRate = 0.18
    Currency = 'PEN'
    CurrencySymbol = 'S/'
    InvoicePrefix = 'FAC'
    PaymentMethods = @('Efectivo','Tarjeta','Yape','Plin','Transferencia')
    LegalText = 'Operación sujeta al IGV (18%).'
}
$rb = Put '/api/settings/billing' $billing
if ($rb.ok) { Log 'OK' "Config facturación" "taxRate=$($rb.data.taxRate) medios=$($rb.data.paymentMethods -join ',')" } else { Log 'FAIL' 'Config facturación' "$($rb.status) $($rb.error)" }

$agenda = @{ OpeningTime='08:00'; ClosingTime='20:00'; SlotMinutes=30; DefaultDurationMinutes=30; MaxAdvanceDays=90; ClosedWeekdays=@('Sunday') }
$ra = Put '/api/settings/agenda' $agenda
if ($ra.ok) { Log 'OK' "Config agenda" "slot=$($ra.data.slotMinutes)min" } else { Log 'FAIL' 'Config agenda' "$($ra.status) $($ra.error)" }

$inv = @{ ExpiringSoonDays=45; Categories=@('Vacunas','Antibióticos','Antiparasitarios','Antiinflamatorios','Insumos','Alimentos'); Units=@('dosis','ml','tab','sachet','bolsa','unidad'); DefaultMinStock=10 }
$ri = Put '/api/settings/inventory' $inv
if ($ri.ok) { Log 'OK' "Config inventario" "vencimiento=$($ri.data.expiringSoonDays)d" } else { Log 'FAIL' 'Config inventario' "$($ri.status) $($ri.error)" }

$sec = @{ PasswordMinLength=8; RequireLetter=$true; RequireDigit=$true; PinLength=4; SessionHours=12; MaxFailedAttempts=5 }
$rs = Put '/api/settings/security' $sec
if ($rs.ok) { Log 'OK' "Config seguridad" "sessionH=$($rs.data.sessionHours)" } else { Log 'FAIL' 'Config seguridad' "$($rs.status) $($rs.error)" }

$notif = @{ Enabled=$true; SmtpHost='smtp.huellitas.pe'; SmtpPort=587; SmtpUser='notificaciones@huellitas.pe'; SmtpPassword='demo-pass'; UseTls=$true; FromName='Huellitas del Perú'; FromEmail='notificaciones@huellitas.pe'; Channels=@('Email'); VaccineReminderDaysBefore=7 }
$rn = Put '/api/settings/notifications' $notif
if ($rn.ok) { Log 'OK' "Config notificaciones" "enabled=$($rn.data.enabled)" } else { Log 'FAIL' 'Config notificaciones' "$($rn.status) $($rn.error)" }
$rt = Post '/api/settings/notifications/test' @{}
if ($rt.ok) { Log 'OK' 'notifications/test' "ok=$($rt.data.ok)" } else { Log 'FAIL' 'notifications/test' "$($rt.status) $($rt.error)" }

$rpub = GetJson '/api/settings/public' @{}
if ($rpub.ok) { Log 'OK' 'settings/public (anónimo)' "name=$($rpub.data.name)" } else { Log 'FAIL' 'settings/public' "$($rpub.status)" }
$rsys = GetJson '/api/settings/system'
if ($rsys.ok) { Log 'OK' 'settings/system' "env=$($rsys.data.environment) db=$($rsys.data.database)" } else { Log 'FAIL' 'settings/system' "$($rsys.status)" }

# =========================================================
# F2 - USUARIOS Y ROLES
# =========================================================
$roles = (GetJson '/api/settings/roles').data
$vetRoleId = ($roles | Where-Object { $_.name -eq 'Veterinario' }).id
$recRoleId = ($roles | Where-Object { $_.name -eq 'Recepcionista' }).id
Log 'OK' "Roles: $($roles.name -join ', ')"

$perms = (GetJson '/api/settings/permissions').data
Log 'OK' "Catálogo de permisos" "total=$($perms.Count)"

$existingUsers = (GetJson '/api/settings/users').data

# vet_andes
$uVet = $existingUsers | Where-Object { $_.username -eq 'vet_andes' }
if ($uVet) { Log 'OK' 'Usuario vet_andes ya existe' "id=$($uVet.id)" }
else {
    $nu = Post '/api/settings/users' @{ Username='vet_andes'; Password='VetAndes123!'; FullName='Andrés Quispe Mamani'; Email='andres@huellitas.pe'; RoleId=$vetRoleId; Pin='4444' }
    if ($nu.ok) { Log 'OK' 'Crear usuario vet_andes' "id=$($nu.data.id)" } else { Log 'FAIL' 'Crear usuario vet_andes' "$($nu.status) $($nu.error)" }
}

# recep_surco
$uRec = $existingUsers | Where-Object { $_.username -eq 'recep_surco' }
if ($uRec) {
    Log 'OK' 'Usuario recep_surco ya existe' "id=$($uRec.id)"
    $up = Put "/api/settings/users/$($uRec.id)" @{ FullName='Paola Rojas Delgado'; Email='paola.rojas@huellitas.pe'; RoleId=$recRoleId; IsActive=$true }
    if ($up.ok) { Log 'OK' 'Actualizar usuario recep_surco' } else { Log 'FAIL' 'Actualizar usuario' "$($up.status) $($up.error)" }
    $rp = Post "/api/settings/users/$($uRec.id)/password" @{ Password='RecepSurco456!' }
    if ($rp.ok) { Log 'OK' 'Reset password recep_surco' } else { Log 'FAIL' 'Reset password' "$($rp.status) $($rp.error)" }
    $sp = Post "/api/settings/users/$($uRec.id)/pin" @{ Pin='5555' }
    if ($sp.ok) { Log 'OK' 'Set pin recep_surco' } else { Log 'FAIL' 'Set pin' "$($sp.status) $($sp.error)" }
} else {
    $nu2 = Post '/api/settings/users' @{ Username='recep_surco'; Password='RecepSurco123!'; FullName='Paola Rojas Delgado'; Email='paola@huellitas.pe'; RoleId=$recRoleId; Pin=$null }
    if ($nu2.ok) { Log 'OK' 'Crear usuario recep_surco' "id=$($nu2.data.id)" } else { Log 'FAIL' 'Crear usuario recep_surco' "$($nu2.status) $($nu2.error)" }
}

# rol Auxiliar (idempotente)
$existingRole = $roles | Where-Object { $_.name -eq 'Auxiliar de Peluquería' }
if ($existingRole) { Log 'OK' 'Rol Auxiliar ya existe' "id=$($existingRole.id)" }
else {
    $newRole = Post '/api/settings/roles' @{ Name='Auxiliar de Peluquería'; Permissions=@('appointments.view','patients.view') }
    if ($newRole.ok) { Log 'OK' 'Crear rol Auxiliar' "id=$($newRole.data.id)" } else { Log 'FAIL' 'Crear rol' "$($newRole.status) $($newRole.error)" }
}

# =========================================================
# F3 - TUTORES Y MASCOTAS (Perú, volumen alto)
# =========================================================
$firstNames = @('Luis','María','José','Ana','Carlos','Rosa','Jorge','Carmen','Miguel','Elena','Pedro','Sofía','Juan','Lucía','Diego','Valeria','Andrés','Paola','Ricardo','Gabriela','Fernando','Daniela','Alberto','Mónica','Sergio','Verónica','Hugo','Patricia','Raúl','Claudia')
$lastNames = @('Quispe','Mamani','Flores','Huamán','Condori','Chávez','Rojas','Vásquez','Ramos','Castillo','Vargas','Ríos','Paredes','Salazar','Delgado','Espinoza','Cárdenas','Vilca','Yupanqui','Zegarra','Sosa','Medina','Herrera','Aguilar','Rivera','Dávila','Ponce','Palomino','Cruz','Sánchez')
$districts = @('Surco','Miraflores','San Isidro','La Molina','San Borja','Jesús María','Lince','Magdalena','Ate','Villa El Salvador')

$tutorIds = @()
$dniBase = 40123456
for ($i = 0; $i -lt 30; $i++) {
    $fn = $firstNames[$i]; $ln = $lastNames[$i]
    $dni = [string]($dniBase + $i * 137)
    $t = @{
        FirstName = $fn; LastName = $ln
        Email = ("tutor{0:D2}@correo.pe" -f ($i + 1))
        Phone = ('+51 9' + (10000000 + $i * 2468).ToString().Substring(1))
        Address = ("Calle Los Álamos {0}, {1}, Lima" -f (100 + $i * 3), $districts[$i % $districts.Count])
        DocumentNumber = $dni
    }
    $r = Post '/api/patients/tutors' $t
    if ($r.ok) { $tutorIds += $r.data.id; Log 'OK' "Tutor $fn $ln" "dni=$dni id=$($r.data.id)" }
    else { Log 'FAIL' "Tutor $fn $ln" "$($r.status) $($r.error)" }
}

$petDefs = @(
    @('Bruno','Perro','Bulldog Francés'), @('Luna','Gato','Persa'), @('Toby','Perro','Golden Retriever'),
    @('Coco','Ave','Loro'), @('Max','Perro','Pastor Alemán'), @('Nala','Gato','Maine Coon'),
    @('Simba','Perro','Maltés'), @('Kiara','Gato','Bengalí'), @('Rocky','Perro','Labrador'),
    @('Michi','Gato','Siamés'), @('Firulais','Perro','Mestizo'), @('Pelusa','Gato','Angora'),
    @('Rex','Perro','Rottweiler'), @('Milo','Gato','Común Europeo'), @('Boby','Perro','Beagle'),
    @('Estrella','Gato','Siamés'), @('Chispa','Perro','Chihuahua'), @('Duque','Perro','Doberman'),
    @('Nina','Gato','Persa'), @('Zeus','Perro','Husky'), @('Motita','Gato','Tricolor'),
    @('Thor','Perro','Pitbull'), @('Canela','Perro','Cocker'), @('Apolo','Perro','Boxer'),
    @('Lola','Gato','Calicó'), @('Rambo','Perro','Gran Danés'), @('Kitty','Gato','Siamés'),
    @('Dino','Reptil','Iguana'), @('Ruby','Gato','Bengalí'), @('Pancho','Ave','Periquito'),
    @('Goliat','Perro','San Bernardo'), @('Sasha','Gato','Maine Coon'), @('Tito','Exotico','Conejo'),
    @('Kira','Perro','Akita'), @('Bolt','Perro','Border Collie'), @('Perla','Gato','Blanco')
)
$pMap = @{}
$petIds = @()
$idx = 0
foreach ($pd in $petDefs) {
    $name = $pd[0]; $sp = $pd[1]; $breed = $pd[2]
    $tutorId = $tutorIds[($idx % $tutorIds.Count)]
    $sex = if ($idx % 2 -eq 0) { 'Macho' } else { 'Hembra' }
    $birth = IsoUtc (Get-Date).AddYears(-1 * (2 + ($idx % 8))).AddMonths(-($idx % 11))
    $body = @{
        Name=$name; Species=$sp; Breed=$breed; BirthDate=$birth; Sex=$sex
        WeightKg=[math]::Round(2 + ($idx % 35) * 0.7, 1); Color='Mestizo'
        TutorId=$tutorId; IsSterilized=($idx % 3 -ne 0); PhotoUrl=$null
    }
    $r = Post '/api/patients/pets' $body
    if ($r.ok) { $pMap[$name] = $r.data.id; $petIds += $r.data.id; Log 'OK' "Mascota $name ($sp)" "id=$($r.data.id)" }
    else { Log 'FAIL' "Mascota $name" "$($r.status) $($r.error)" }
    $idx++
}
# mascota inactiva para probar filtros/includeInactive
if ($petIds.Count -gt 0) {
    $inactive = Put "/api/patients/pets/$($petIds[-1])/active" @{ Active=$false }
    if ($inactive.ok) { Log 'OK' 'Desactivar mascota (para includeInactive)' "id=$($petIds[-1])" } else { Log 'FAIL' 'Desactivar mascota' "$($inactive.status)" }
}
if ($tutorIds.Count -eq 0) { throw 'No se creó ningún tutor; abortando (revisar errores previos).' }
if ($petIds.Count -eq 0) { throw 'No se creó ninguna mascota; abortando (revisar errores previos).' }

# =========================================================
# F4 - INVENTARIO + LOTES + KARDEX
# =========================================================
$items = @(
    @{ Name='Vacuna Antirrábica';          Category='Vacunas';          Code='VAC-100'; Unit='dosis';  MinStock=10; UnitPrice=25.0;  Supplier='BioVet Perú';  IsFractionable=$false },
    @{ Name='Vacuna Polivalente Canina';   Category='Vacunas';          Code='VAC-101'; Unit='dosis';  MinStock=10; UnitPrice=35.0;  Supplier='BioVet Perú';  IsFractionable=$false },
    @{ Name='Vacuna Polivalente Felina';   Category='Vacunas';          Code='VAC-102'; Unit='dosis';  MinStock=8;  UnitPrice=30.0;  Supplier='BioVet Perú';  IsFractionable=$false },
    @{ Name='Amoxicilina 250mg';           Category='Antibióticos';     Code='MED-100'; Unit='tab';    MinStock=20; UnitPrice=1.5;   Supplier='FarmaVet';     IsFractionable=$false },
    @{ Name='Metronidazol 125mg';          Category='Antibióticos';     Code='MED-101'; Unit='tab';    MinStock=20; UnitPrice=1.2;   Supplier='FarmaVet';     IsFractionable=$false },
    @{ Name='Ivermectina 1%';              Category='Antiparasitarios'; Code='MED-102'; Unit='ml';     MinStock=50; UnitPrice=0.8;   Supplier='FarmaVet';     IsFractionable=$true  },
    @{ Name='Ketoprofeno 10%';             Category='Antiinflamatorios';Code='MED-103'; Unit='ml';     MinStock=20; UnitPrice=18.0;  Supplier='FarmaVet';     IsFractionable=$true  },
    @{ Name='Suero Fisiológico 500ml';     Category='Insumos';          Code='INS-100'; Unit='sachet'; MinStock=15; UnitPrice=6.5;   Supplier='MediSupply';   IsFractionable=$false },
    @{ Name='Gasas Estériles';             Category='Insumos';          Code='INS-101'; Unit='unidad'; MinStock=50; UnitPrice=0.5;   Supplier='MediSupply';   IsFractionable=$false },
    @{ Name='Pipeta Antipulgas';           Category='Antiparasitarios'; Code='MED-104'; Unit='dosis';  MinStock=15; UnitPrice=45.0;  Supplier='VetGuard';     IsFractionable=$false },
    @{ Name='Croquetas Adulto 3kg';        Category='Alimentos';        Code='ALI-100'; Unit='bolsa';  MinStock=5;  UnitPrice=120.0; Supplier='NutriPet';     IsFractionable=$false },
    @{ Name='Shampoo Antipulgas 250ml';    Category='Insumos';          Code='INS-102'; Unit='unidad'; MinStock=10; UnitPrice=22.0;  Supplier='VetGuard';     IsFractionable=$false }
)
$iMap = @{}
$iInfo = @{}
foreach ($it in $items) {
    $r = Post '/api/inventory' $it
    if ($r.ok) { $iMap[$it.Code] = $r.data.id; $iInfo[$it.Code] = $it; Log 'OK' "Item $($it.Code) $($it.Name)" "id=$($r.data.id)" }
    else { Log 'FAIL' "Item $($it.Code)" "$($r.status) $($r.error)" }
}

# lotes: vencido (-10d), próximo (+20/+40d), lejano, y con stock amplio para ~60 facturas
$lots = @(
    @{ Code='VAC-100'; Lot='LOT-RAB-1'; Exp=25;  Qty=200 },
    @{ Code='VAC-101'; Lot='LOT-POL-1'; Exp=40;  Qty=200 },
    @{ Code='VAC-101'; Lot='LOT-POL-0'; Exp=-10; Qty=5   },
    @{ Code='VAC-102'; Lot='LOT-FEL-1'; Exp=90;  Qty=150 },
    @{ Code='MED-100'; Lot='LOT-AMOX-1'; Exp=300; Qty=800 },
    @{ Code='MED-101'; Lot='LOT-MET-1'; Exp=200; Qty=800 },
    @{ Code='MED-102'; Lot='LOT-IVM-0'; Exp=-5;  Qty=100 },
    @{ Code='MED-102'; Lot='LOT-IVM-1'; Exp=35;  Qty=500 },
    @{ Code='MED-102'; Lot='LOT-IVM-2'; Exp=400; Qty=5000 },
    @{ Code='MED-103'; Lot='LOT-KET-1'; Exp=120; Qty=1000 },
    @{ Code='INS-100'; Lot='LOT-SUERO-1'; Exp=365; Qty=500 },
    @{ Code='INS-101'; Lot='LOT-GASA-1';  Exp=500; Qty=2000 },
    @{ Code='MED-104'; Lot='LOT-PIP-1';   Exp=250; Qty=300 },
    @{ Code='ALI-100'; Lot='LOT-CROQ-1';  Exp=180; Qty=300 },
    @{ Code='INS-102'; Lot='LOT-SHMP-1';  Exp=600; Qty=200 }
)
$lotMap = @{}
foreach ($l in $lots) {
    $body = @{ LotNumber=$l.Lot; ExpirationDate=(IsoUtc (Get-Date).AddDays($l.Exp)); Quantity=$l.Qty; Description="Lote demo Perú ($($l.Code))" }
    $r = Post "/api/inventory/$($iMap[$l.Code])/lots" $body
    if ($r.ok) { $lotMap[$l.Lot] = $r.data.id; Log 'OK' "Lote $($l.Lot)" "id=$($r.data.id)" }
    else { Log 'FAIL' "Lote $($l.Lot)" "$($r.status) $($r.error)" }
}

# Kardex directo
$a1 = Patch "/api/inventory/$($iMap['INS-101'])/stock" @{ Quantity=100; Description='Compra directa sin lote' }
if ($a1.ok) { Log 'OK' 'PATCH stock +100 INS-101' "stock=$($a1.data.stockQuantity)" } else { Log 'FAIL' 'PATCH stock INS-101' "$($a1.status) $($a1.error)" }
$a2 = Patch "/api/inventory/$($iMap['MED-101'])/stock" @{ Quantity=-5; Description='Uso interno en consulta' }
if ($a2.ok) { Log 'OK' 'PATCH stock -5 MED-101' "stock=$($a2.data.stockQuantity)" } else { Log 'FAIL' 'PATCH stock MED-101' "$($a2.status) $($a2.error)" }

# Desactivar/reactivar lote (cobertura de endpoints) sobre un lote lejano
$tmpLot = $lotMap['LOT-CROQ-1']
if ($tmpLot) {
    $dl = Delete "/api/inventory/lots/$tmpLot"
    if ($dl.ok) { Log 'OK' 'DELETE lote (desactivar)' "lotId=$tmpLot" } else { Log 'FAIL' 'DELETE lote' "$($dl.status) $($dl.error)" }
    $al = Patch "/api/inventory/lots/$tmpLot/activate" @{}
    if ($al.ok) { Log 'OK' 'PATCH lote activar' "lotId=$tmpLot" } else { Log 'FAIL' 'PATCH lote activar' "$($al.status) $($al.error)" }
    $il = GetJson "/api/inventory/$($iMap['ALI-100'])/lots?includeInactive=true"
    $hasInactive = @($il.data).Count -ge 1
    Log $(if ($hasInactive) { 'OK' } else { 'FAIL' }) 'GET lots includeInactive' "count=$(@($il.data).Count)"
}

# Filtros de inventario
$fSearch = GetJson '/api/inventory?search=vacuna'
if ($fSearch.ok) { Log 'OK' 'GET inventory?search=vacuna' "count=$(@($fSearch.data).Count)" } else { Log 'FAIL' 'GET inventory search' "$($fSearch.status)" }
$fCat = GetJson "/api/inventory?category=$([uri]::EscapeDataString('Antibióticos'))"
if ($fCat.ok) { Log 'OK' 'GET inventory?category=Antibióticos' "count=$(@($fCat.data).Count)" } else { Log 'FAIL' 'GET inventory category' "$($fCat.status)" }
$fLow = GetJson '/api/inventory?lowOnly=true'
if ($fLow.ok) { Log 'OK' 'GET inventory?lowOnly=true' "count=$(@($fLow.data).Count)" } else { Log 'FAIL' 'GET inventory lowOnly' "$($fLow.status)" }
$fExp = GetJson '/api/inventory/lots/expiring?days=60'
if ($fExp.ok) { Log 'OK' 'GET lots/expiring' "count=$(@($fExp.data).Count)" } else { Log 'FAIL' 'GET lots/expiring' "$($fExp.status)" }
$mv = GetJson "/api/inventory/$($iMap['INS-101'])/movements"
if ($mv.ok) { Log 'OK' 'GET movements INS-101' "count=$(@($mv.data).Count)" } else { Log 'FAIL' 'GET movements' "$($mv.status)" }

# =========================================================
# F5 - CITAS (variedad estados/urgencias + filtros)
# =========================================================
$reasons = @('Control post-vacunación','Revisión anual senior','Diarrea persistente','Ingestión de objeto extraño','Vacuna polivalente','Cojera','Dermatitis','Tos persistente','Chequeo general','Limpieza dental')
$urgencies = @('Baja','Media','Alta','Emergencia')
$apptIds = @()
$created = 0
$dayCursor = 1
while ($created -lt 30) {
    $d = (Get-Date).AddDays($dayCursor).Date
    $dayCursor++
    if ($d.DayOfWeek -eq [System.DayOfWeek]::Sunday) { continue }
    foreach ($hr in @(10,14)) {
        if ($created -ge 30) { break }
        $i = $created
        $petId = $petIds[$i % $petIds.Count]
        $start = $d.AddHours($hr).AddMinutes(($i % 2) * 30)
        $body = @{
            StartTime = (IsoUtc $start)
            DurationMinutes = 30
            Urgency = $urgencies[$i % $urgencies.Count]
            Reason = $reasons[$i % $reasons.Count]
            PetId = $petId
            VetId = $vetId
        }
        $r = Post '/api/appointments' $body
        if ($r.ok) { $apptIds += $r.data.id; Log 'OK' "Cita #$($i+1) $($body.Reason)" "id=$($r.data.id) urg=$($body.Urgency)" }
        else { Log 'FAIL' "Cita #$($i+1)" "$($r.status) $($r.error)" }
        $created++
    }
}

# Transiciones de estado (cubrir todos los estados)
$transitions = @(
    @{ Idx=0; Steps=@('EnEspera','EnConsulta','Completada') },
    @{ Idx=1; Steps=@('EnEspera','EnConsulta') },
    @{ Idx=2; Steps=@('Cancelada') },
    @{ Idx=3; Steps=@('NoAsistio') },
    @{ Idx=4; Steps=@('EnConsulta','Completada') }
)
foreach ($tr in $transitions) {
    if ($apptIds.Count -le $tr.Idx) { continue }
    $id = $apptIds[$tr.Idx]
    foreach ($st in $tr.Steps) {
        $r = Patch "/api/appointments/$id/status" @{ Status=$st; Notes="Transición a $st" }
        if ($r.ok) { Log 'OK' "Cita $id -> $st" } else { Log 'FAIL' "Cita $id -> $st" "$($r.status) $($r.error)" }
    }
}

# Reprogramar (PUT) una cita futura
if ($apptIds.Count -gt 12) {
    $rid = $apptIds[12]
    $rd = (Get-Date).AddDays(5).Date
    while ($rd.DayOfWeek -eq [System.DayOfWeek]::Sunday) { $rd = $rd.AddDays(1) }
    $newStart = $rd.AddHours(13)
    $ru = Put "/api/appointments/$rid" @{ StartTime=(IsoUtc $newStart); DurationMinutes=45; Urgency='Media'; Reason='Reprogramada por el tutor'; PetId=$petIds[0]; VetId=$vetId }
    if ($ru.ok) { Log 'OK' 'PUT reprogramar cita' "id=$rid dur=$($ru.data.durationMinutes)" } else { Log 'FAIL' 'PUT reprogramar cita' "$($ru.status) $($ru.error)" }
}

# Filtros / listados
$from = [uri]::EscapeDataString((IsoUtc (Get-Date).AddDays(-30)))
$to = [uri]::EscapeDataString((IsoUtc (Get-Date).AddDays(30)))
$l1 = GetJson "/api/appointments?from=$from&to=$to"
if ($l1.ok) { Log 'OK' 'GET appointments?from/to' "count=$(@($l1.data).Count)" } else { Log 'FAIL' 'GET appointments from/to' "$($l1.status)" }
$l2 = GetJson '/api/appointments?status=Completada'
if ($l2.ok) { Log 'OK' 'GET appointments?status=Completada' "count=$(@($l2.data).Count)" } else { Log 'FAIL' 'GET appointments status' "$($l2.status)" }
$l3 = GetJson "/api/appointments?vetId=$vetId"
if ($l3.ok) { Log 'OK' 'GET appointments?vetId' "count=$(@($l3.data).Count)" } else { Log 'FAIL' 'GET appointments vetId' "$($l3.status)" }
$l4 = GetJson "/api/appointments/pet/$($petIds[0])"
if ($l4.ok) { Log 'OK' 'GET appointments/pet/{id}' "count=$(@($l4.data).Count)" } else { Log 'FAIL' 'GET appointments/pet' "$($l4.status)" }
$l5 = GetJson '/api/appointments/upcoming'
if ($l5.ok) { Log 'OK' 'GET appointments/upcoming' "count=$(@($l5.data).Count)" } else { Log 'FAIL' 'GET appointments/upcoming' "$($l5.status)" }

# =========================================================
# F6 - HISTORIAS CLÍNICAS (MR + recetas + labs + adjuntos)
# =========================================================
$mrPetSample = $petIds | Select-Object -First 8
$mrIds = @()
$mrIdx = 0
foreach ($petId in $mrPetSample) {
    $body = @{
        PetId=$petId; VetId=$vetId; AppointmentId=$null
        Symptoms='Decaimiento y tos'; Findings='RU transitorios, fiebre 39.5'
        Diagnosis='Traqueobronquitis leve'; TreatmentPlan='Reposo y amoxicilina 7 días'
        WeightKg=[math]::Round(5 + ($mrIdx % 20), 1); TemperatureC=39.5
    }
    $r = Post '/api/medical-records' $body
    if ($r.ok) { $mrIds += $r.data.id; Log 'OK' "MR mascota $petId" "id=$($r.data.id)" }
    else { Log 'FAIL' "MR mascota $petId" "$($r.status) $($r.error)" }
    $mrIdx++
}

if ($mrIds.Count -ge 2) {
    $m0 = $mrIds[0]
    $pr = Post "/api/medical-records/$m0/prescriptions" @{ MedicationName='Amoxicilina 250mg'; Dosage='1 tab cada 12h'; Frequency='cada 12 horas'; Duration='7 días'; Instructions='Administrar con comida' }
    $prId = $null
    if ($pr.ok) { $prId = $pr.data.prescriptions[-1].id; Log 'OK' 'Receta MR0' } else { Log 'FAIL' 'Receta MR0' "$($pr.status) $($pr.error)" }
    if ($prId) {
        $prU = Put "/api/medical-records/$m0/prescriptions/$prId" @{ MedicationName='Amoxicilina 250mg'; Dosage='1 tab cada 8h'; Frequency='cada 8 horas'; Duration='5 días'; Instructions='Con alimento' }
        if ($prU.ok) { Log 'OK' 'PUT receta MR0' } else { Log 'FAIL' 'PUT receta MR0' "$($prU.status) $($prU.error)" }
        $prD = Delete "/api/medical-records/$m0/prescriptions/$prId"
        if ($prD.ok) { Log 'OK' 'DELETE receta MR0' } else { Log 'FAIL' 'DELETE receta MR0' "$($prD.status) $($prD.error)" }
    }

    $m1 = $mrIds[1]
    $lb = Post "/api/medical-records/$m1/lab-results" @{ TestType='Hematología'; Result='Leucocitos 22.000/µL'; ReferenceRange='6.000-17.000/µL'; IsAbnormal=$true }
    $lbId = $null
    if ($lb.ok) { $lbId = $lb.data.labResults[-1].id; Log 'OK' 'Lab anormal MR1' } else { Log 'FAIL' 'Lab MR1' "$($lb.status) $($lb.error)" }
    if ($lbId) {
        $lbU = Put "/api/medical-records/$m1/lab-results/$lbId" @{ TestType='Hematología'; Result='Leucocitos 15.000/µL'; ReferenceRange='6.000-17.000/µL'; IsAbnormal=$false }
        if ($lbU.ok) { Log 'OK' 'PUT lab MR1' } else { Log 'FAIL' 'PUT lab MR1' "$($lbU.status) $($lbU.error)" }
        $lbD = Delete "/api/medical-records/$m1/lab-results/$lbId"
        if ($lbD.ok) { Log 'OK' 'DELETE lab MR1' } else { Log 'FAIL' 'DELETE lab MR1' "$($lbD.status) $($lbD.error)" }
    }

    # Adjuntos: subir / descargar / borrar
    $tmpFile = Join-Path $env:TEMP 'vetclinic_demo_radiografia.txt'
    Set-Content -Path $tmpFile -Value 'Informe radiográfico demo - Huellitas del Perú'
    $up = PostFile "/api/medical-records/$m0/attachments" $tmpFile 'text/plain'
    if ($up.ok) {
        $attId = $up.data.attachments[-1].id
        Log 'OK' 'POST adjunto MR0' "id=$attId"
        $dl = TestDownload "/api/medical-records/$m0/attachments/$attId/download?download=true"
        Log $(if ($dl.status -eq 200 -and $dl.length -gt 0) { 'OK' } else { 'FAIL' }) 'GET descargar adjunto' "status=$($dl.status) bytes=$($dl.length)"
        $atD = Delete "/api/medical-records/$m0/attachments/$attId"
        if ($atD.ok) { Log 'OK' 'DELETE adjunto MR0' } else { Log 'FAIL' 'DELETE adjunto' "$($atD.status) $($atD.error)" }
    } else { Log 'FAIL' 'POST adjunto MR0' "$($up.status) $($up.error)" }
}

# =========================================================
# F7 - VACUNAS
# =========================================================
$vacIds = @('VAC-100','VAC-101','VAC-102')
for ($i = 0; $i -lt 20; $i++) {
    $petId = $petIds[$i % $petIds.Count]
    $code = $vacIds[$i % $vacIds.Count]
    $appDate = (IsoUtc (Get-Date).AddDays(-1 * (5 + $i)))
    $next = if ($i % 4 -eq 0) { $null } else { (IsoUtc (Get-Date).AddDays(2 + ($i % 40))) }
    $body = @{ InventoryItemId=$iMap[$code]; ApplicationDate=$appDate; NextDueDate=$next }
    $r = Post "/api/patients/pets/$petId/vaccines" $body
    if ($r.ok) { Log 'OK' "Vacuna pet=$petId $code" "isDueSoon=$($r.data.isDueSoon)" }
    else { Log 'FAIL' "Vacuna pet=$petId" "$($r.status) $($r.error)" }
}
$vu = GetJson '/api/patients/vaccines/upcoming'
if ($vu.ok) { Log 'OK' 'GET vaccines/upcoming' "count=$(@($vu.data).Count)" } else { Log 'FAIL' 'GET vaccines/upcoming' "$($vu.status)" }
$vp = GetJson "/api/patients/pets/$($petIds[0])/vaccines"
if ($vp.ok) { Log 'OK' 'GET pets/{id}/vaccines' "count=$(@($vp.data).Count)" } else { Log 'FAIL' 'GET pets vaccines' "$($vp.status)" }

# =========================================================
# F8 - HOSPITALIZACIONES
# =========================================================
# Activas (3 casos clínicos distintos)
$hospCases = @(
    @{ Reason='Gastroenteritis aguda'; Notes='Requiere fluidoterapia IV'
       E1=@{ Temperature=39.2; HeartRate=120; RespiratoryRate=30; Observations='Deshidratación moderada, fluidos en curso' }
       E2=@{ Temperature=38.8; HeartRate=110; RespiratoryRate=26; Observations='Mejoría, tolera alimento' }
       Med=@{ MedicationName='Metoclopramida 5mg'; Dose='0.5 mg/kg'; Route='IV' } }
    @{ Reason='Trauma por caída'; Notes='Analgesia y reposo'
       E1=@{ Temperature=38.5; HeartRate=112; RespiratoryRate=28; Observations='Herida suturada, paciente estable' }
       E2=@{ Temperature=38.4; HeartRate=104; RespiratoryRate=24; Observations='Dolor controlado' }
       Med=@{ MedicationName='Meloxicam 2mg'; Dose='0.1 mg/kg'; Route='SC' } }
    @{ Reason='Crisis respiratoria'; Notes='Oxigenoterapia y control'
       E1=@{ Temperature=39.4; HeartRate=130; RespiratoryRate=36; Observations='Patrón respiratorio irregular' }
       E2=@{ Temperature=38.9; HeartRate=118; RespiratoryRate=28; Observations='Respiración más regular' }
       Med=@{ MedicationName='Furosemida 10mg'; Dose='2 mg/kg'; Route='IV' } }
)
for ($i = 0; $i -lt $hospCases.Count; $i++) {
    $petId = $petIds[($i + 3) % $petIds.Count]
    $c = $hospCases[$i]
    $hosp = Post '/api/hospitalizations' @{ PetId=$petId; AssignedVetId=$vetId; Reason=$c.Reason; Notes=$c.Notes }
    if ($hosp.ok) {
        $hid = $hosp.data.id
        Log 'OK' "Hospitalización activa pet=$petId" "id=$hid motivo=$($c.Reason)"
        $e1 = Post "/api/hospitalizations/$hid/evolutions" $c.E1
        $e2 = Post "/api/hospitalizations/$hid/evolutions" $c.E2
        $m1 = Post "/api/hospitalizations/$hid/medications" ($c.Med + @{ AdministeredAt=(IsoUtc (Get-Date)) })
        if ($e1.ok -and $e2.ok -and $m1.ok) { Log 'OK' "Evoluciones + medicación $hid" } else { Log 'FAIL' "Evoluciones $hid" 'revisar' }
        $st = Patch "/api/hospitalizations/$hid/status" @{ Status='EnTratamiento' }
        if ($st.ok) { Log 'OK' "PATCH status $hid -> EnTratamiento" } else { Log 'FAIL' "PATCH status $hid" "$($st.status) $($st.error)" }
    } else { Log 'FAIL' "Hospitalización pet=$petId" "$($hosp.status) $($hosp.error)" }
}
# Con alta
$hd = Post '/api/hospitalizations' @{ PetId=$petIds[1]; AssignedVetId=$vetId; Reason='Observación post procedimiento'; Notes='Inicio de alta' }
if ($hd.ok) {
    $hid = $hd.data.id
    $e3 = Post "/api/hospitalizations/$hid/evolutions" @{ Temperature=38.4; HeartRate=80; RespiratoryRate=20; Observations='Recuperación sin novedades' }
    $dc = Post "/api/hospitalizations/$hid/discharge" @{}
    if ($dc.ok) { Log 'OK' "Alta hospitalización $hid" "status=$($dc.data.status)" } else { Log 'FAIL' "Alta $hid" "$($dc.status) $($dc.error)" }
} else { Log 'FAIL' 'Hospitalización alta' "$($hd.status) $($hd.error)" }

$ha = GetJson '/api/hospitalizations/active'
if ($ha.ok) { Log 'OK' 'GET hospitalizations/active' "count=$(@($ha.data).Count)" } else { Log 'FAIL' 'GET hospitalizations/active' "$($ha.status)" }
$hl = GetJson '/api/hospitalizations'
if ($hl.ok) { Log 'OK' 'GET hospitalizations' "count=$(@($hl.data).Count)" } else { Log 'FAIL' 'GET hospitalizations' "$($hl.status)" }

# =========================================================
# F9 - FACTURAS (~60) + BACKDATE POR SQL
# =========================================================
$payments = @('Efectivo','Tarjeta','Yape','Plin','Transferencia')
$invoiceables = @(
    @{ Code='MED-102'; Desc='Ivermectina 1%';          UnitPrice=0.8;  Frac=$true  },
    @{ Code='INS-100'; Desc='Suero Fisiológico 500ml'; UnitPrice=6.5;  Frac=$false },
    @{ Code='MED-100'; Desc='Amoxicilina 250mg';       UnitPrice=1.5;  Frac=$false },
    @{ Code='INS-101'; Desc='Gasas Estériles';         UnitPrice=0.5;  Frac=$false },
    @{ Code='MED-103'; Desc='Ketoprofeno 10%';         UnitPrice=18.0; Frac=$true  },
    @{ Code='VAC-102'; Desc='Vacuna Polivalente Felina';UnitPrice=30.0; Frac=$false },
    @{ Code='MED-104'; Desc='Pipeta Antipulgas';       UnitPrice=45.0; Frac=$false },
    @{ Code='ALI-100'; Desc='Croquetas Adulto 3kg';    UnitPrice=120.0;Frac=$false }
)
$tutorById = @{}   # id -> tutor id (para asociar factura al dueño de la mascota)
$invoiceIds = @()
$invoiceCount = 60
$expectedTaxRate = 0.18
for ($i = 0; $i -lt $invoiceCount; $i++) {
    $petId = $petIds[$i % $petIds.Count]
    $tutorId = $tutorIds[$i % $tutorIds.Count]
    $status = if ($i % 10 -eq 0) { 'Borrador' } elseif ($i % 7 -eq 0) { 'Emitida' } else { 'Pagada' }
    $lineCount = 1 + ($i % 3)
    $lines = @()
    for ($j = 0; $j -lt $lineCount; $j++) {
        $inv = $invoiceables[($i + $j) % $invoiceables.Count]
        $qty = if ($inv.Frac) { [math]::Round(1 + ($j * 0.5), 1) } else { 1 + ($j % 3) }
        $spec = @{ Description=$inv.Desc; Quantity=$qty; UnitPrice=$inv.UnitPrice; InventoryItemId=$iMap[$inv.Code] }
        $lines += $spec
    }
    $body = @{ TutorId=$tutorId; PetId=$petId; Status=$status; PaymentMethod=$payments[$i % $payments.Count]; Lines=$lines }
    $r = Post '/api/invoices' $body
    if ($r.ok) {
        $invoiceIds += $r.data.id
        $expTax = [math]::Round([decimal]$r.data.subtotal * $expectedTaxRate, 2)
        $okTax = ([decimal]$r.data.taxes -eq $expTax)
        Log $(if ($okTax) { 'OK' } else { 'FAIL' }) "Factura $($r.data.invoiceNumber) ($status)" "total=$($r.data.total) igv=$($r.data.taxes)/esp=$expTax"
    } else {
        Log 'FAIL' "Factura #$($i+1)" "$($r.status) $($r.error)"
    }
}

# Transiciones de factura (cubrir AllowedTransitions)
if ($invoiceIds.Count -ge 3) {
    $b = Post '/api/invoices' @{ TutorId=$tutorIds[0]; PetId=$petIds[0]; Status='Borrador'; PaymentMethod='Efectivo'; Lines=@(@{ Description='Servicio de peluquería'; Quantity=1; UnitPrice=50; InventoryItemId=$null }) }
    if ($b.ok) {
        $bid = $b.data.id
        $s1 = Patch "/api/invoices/$bid/status" @{ Status='Emitida' }
        if ($s1.ok) { Log 'OK' "Factura $bid Borrador->Emitida" } else { Log 'FAIL' "Factura $bid ->Emitida" "$($s1.status) $($s1.error)" }
        $s2 = Patch "/api/invoices/$bid/status" @{ Status='Pagada' }
        if ($s2.ok) { Log 'OK' "Factura $bid Emitida->Pagada" } else { Log 'FAIL' "Factura $bid ->Pagada" "$($s2.status) $($s2.error)" }
        $s3 = Patch "/api/invoices/$bid/status" @{ Status='Emitida' }
        if ($s3.ok) { Log 'FAIL' 'F rechazo transición Pagada->Emitida' 'se permitió cuando debía rechazarse' } else { Log 'EXPECTED' 'F rechazo transición Pagada->Emitida' "$($s3.status) $($s3.error)" }
    } else { Log 'FAIL' 'Factura transición' "$($b.status) $($b.error)" }
}

# Una factura anulada
$an = Post '/api/invoices' @{ TutorId=$tutorIds[1]; PetId=$petIds[1]; Status='Anulada'; PaymentMethod='Transferencia'; Lines=@(@{ Description='Consulta anulada'; Quantity=1; UnitPrice=80; InventoryItemId=$null }) }
if ($an.ok) { Log 'OK' 'Factura Anulada' "id=$($an.data.id)" } else { Log 'FAIL' 'Factura Anulada' "$($an.status) $($an.error)" }

# Guardas esperadas
$fx1 = Post '/api/invoices' @{ TutorId=$tutorIds[2]; PetId=$petIds[2]; Status='Emitida'; PaymentMethod='Efectivo'; Lines=@(@{ Description='Fracción en no-fraccionable'; Quantity=1.5; UnitPrice=30; InventoryItemId=$iMap['VAC-102'] }) }
if ($fx1.ok) { Log 'FAIL' 'F rechazo fracción no-fraccionable' 'se creó cuando debía rechazarse' } else { Log 'EXPECTED' 'F rechazo fracción no-fraccionable' "$($fx1.status) $($fx1.error)" }
$fx2 = Post '/api/invoices' @{ TutorId=$tutorIds[3]; PetId=$petIds[3]; Status='Emitida'; PaymentMethod='Cripto'; Lines=@(@{ Description='Medio inválido'; Quantity=1; UnitPrice=10; InventoryItemId=$null }) }
if ($fx2.ok) { Log 'FAIL' 'F rechazo medio de pago inválido' 'se creó cuando debía rechazarse' } else { Log 'EXPECTED' 'F rechazo medio de pago inválido' "$($fx2.status) $($fx2.error)" }

# FEFO: el lote vencido de Ivermectina (LOT-IVM-0) debe consumirse antes que LOT-IVM-1/-2
$lotsIvm = (GetJson "/api/inventory/$($iMap['MED-102'])/lots").data
$l0 = @($lotsIvm) | Where-Object { $_.lotNumber -eq 'LOT-IVM-0' }
$l1 = @($lotsIvm) | Where-Object { $_.lotNumber -eq 'LOT-IVM-1' }
$l2 = @($lotsIvm) | Where-Object { $_.lotNumber -eq 'LOT-IVM-2' }
$fefoOk = ([decimal]$l0.availableQuantity -lt 100) -and ([decimal]$l1.availableQuantity -eq 500) -and ([decimal]$l2.availableQuantity -eq 5000)
Log $(if ($fefoOk) { 'OK' } else { 'FAIL' }) 'FEFO' "vencido primero: ivm0=$($l0.availableQuantity)/100 ivm1=$($l1.availableQuantity)/500 ivm2=$($l2.availableQuantity)/5000"

# BACKDATE por SQL: repartir IssueDate en ~6 meses para los reportes
if ($invoiceIds.Count -gt 0) {
    $pairs = @()
    for ($k = 0; $k -lt $invoiceIds.Count; $k++) {
        $months = $k % 6
        $pairs += "($($invoiceIds[$k]),$months)"
    }
    $values = $pairs -join ','
    $sql = "UPDATE ""Invoices"" SET ""IssueDate"" = now() - (m || ' months')::interval, ""UpdatedAt"" = now() FROM (VALUES $values) AS t(id,m) WHERE ""Invoices"".""Id"" = t.id;"
    $rbk = ExecSql $sql
    Log $(if ($rbk.ok) { 'OK' } else { 'FAIL' }) 'Backdate de facturas por SQL' "facturas=$($invoiceIds.Count) meses=6"
}
$invList = GetJson '/api/invoices'
if ($invList.ok) { Log 'OK' 'GET invoices' "count=$(@($invList.data).Count)" } else { Log 'FAIL' 'GET invoices' "$($invList.status)" }

# =========================================================
# F10 - RECORDATORIOS (por SQL, IsSent=false)
# =========================================================
$rPairs = @()
for ($k = 0; $k -lt 5; $k++) {
    $petId = $petIds[$k % $petIds.Count]
    $msg = "Recordatorio: la vacuna de la mascota vence pronto (demo)."
    $rPairs += "($petId,'tutor$k@correo.pe','Email','$msg', now() - interval '$k days', false, now())"
}
$rValues = $rPairs -join ','
$remSql = "INSERT INTO ""Reminders"" (""PetId"",""Recipient"",""Channel"",""Message"",""ScheduledAt"",""IsSent"",""CreatedAt"") VALUES $rValues;"
$rr = ExecSql $remSql
Log $(if ($rr.ok) { 'OK' } else { 'FAIL' }) 'Insertar recordatorios por SQL' 'count=5'

# =========================================================
# F11 - REPORTES + MATRIZ DE PERMISOS
# =========================================================
$revFrom = [uri]::EscapeDataString((IsoUtc (Get-Date).AddMonths(-6)))
$revTo = [uri]::EscapeDataString((IsoUtc (Get-Date).AddDays(1)))

$dash = GetJson "/api/reports/dashboard?from=$revFrom&to=$revTo"
if ($dash.ok -and $dash.data.invoicesCount -gt 0 -and $dash.data.revenue -gt 0) {
    Log 'OK' 'reports/dashboard' "revenue=$($dash.data.revenue) facturas=$($dash.data.invoicesCount) lowStock=$($dash.data.lowStockItems)"
} else { Log 'FAIL' 'reports/dashboard' "status=$($dash.status) rev=$($dash.data.revenue)" }

$rev = GetJson "/api/reports/revenues?from=$revFrom&to=$revTo"
if ($rev.ok -and @($rev.data.byMonth).Count -ge 2) {
    Log 'OK' 'reports/revenues' "total=$($rev.data.total) meses=$(@($rev.data.byMonth).Count)"
} else { Log 'FAIL' 'reports/revenues' "status=$($rev.status) meses=$(@($rev.data.byMonth).Count)" }

$ret = GetJson '/api/reports/retention'
if ($ret.ok -and $ret.data.returning -gt 0) {
    Log 'OK' 'reports/retention' "total=$($ret.data.totalClients) returning=$($ret.data.returning) tasa=$($ret.data.retentionRate)%"
} else { Log 'FAIL' 'reports/retention' "status=$($ret.status) returning=$($ret.data.returning)" }

$ops = GetJson '/api/reports/operations'
if ($ops.ok) {
    Log $(if ($ops.data.pendingReminders -gt 0) { 'OK' } else { 'FAIL' }) 'reports/operations pendingReminders' "pending=$($ops.data.pendingReminders)"
    Log $(if ($ops.data.unpaidInvoicesCount -gt 0) { 'OK' } else { 'FAIL' }) 'reports/operations unpaid' "count=$($ops.data.unpaidInvoicesCount) monto=$($ops.data.unpaidInvoicesAmount)"
} else { Log 'FAIL' 'reports/operations' "$($ops.status)" }

$ls = GetJson '/api/reports/low-stock'
if ($ls.ok) { Log 'OK' 'reports/low-stock' "count=$(@($ls.data).Count)" } else { Log 'FAIL' 'reports/low-stock' "$($ls.status)" }

# Matriz de permisos por rol
$vetLogin = Login 'vet' 'Vet123!'
$recLogin = Login 'recepcion' 'Recep123!'
if ($vetLogin.ok) { Log 'OK' 'Login vet' } else { Log 'FAIL' 'Login vet' "$($vetLogin.status) $($vetLogin.error)" }
if ($recLogin.ok) { Log 'OK' 'Login recepcion' } else { Log 'FAIL' 'Login recepcion' "$($recLogin.status) $($recLogin.error)" }

$vetH = @{ Authorization = "Bearer $($vetLogin.token)" }
$recH = @{ Authorization = "Bearer $($recLogin.token)" }
$anonH = @{}

# 401 sin token
$n1 = GetJson '/api/appointments/config' $anonH
Log $(if ($n1.status -eq 401) { 'EXPECTED' } else { 'FAIL' }) 'Sin token -> 401' "status=$($n1.status)"
# recepcion puede ver agenda
$n2 = GetJson '/api/appointments/config' $recH
Log $(if ($n2.status -eq 200) { 'OK' } else { 'FAIL' }) 'recepcion agenda/config -> 200' "status=$($n2.status)"
# vet/recepcion NO pueden configuracion
$n3 = GetJson '/api/settings/clinic' $recH
Log $(if ($n3.status -eq 403) { 'EXPECTED' } else { 'FAIL' }) 'recepcion /settings/clinic -> 403' "status=$($n3.status)"
$n4 = GetJson '/api/settings/clinic' $vetH
Log $(if ($n4.status -eq 403) { 'EXPECTED' } else { 'FAIL' }) 'vet /settings/clinic -> 403' "status=$($n4.status)"
# admin puede
$n5 = GetJson '/api/settings/clinic'
Log $(if ($n5.status -eq 200) { 'OK' } else { 'FAIL' }) 'admin /settings/clinic -> 200' "status=$($n5.status)"
# reportes por rol
$n6 = GetJson '/api/reports/dashboard?from=2026-01-01&to=2026-12-31' $recH
Log $(if ($n6.status -eq 403) { 'EXPECTED' } else { 'FAIL' }) 'recepcion dashboard -> 403' "status=$($n6.status)"
$n7 = GetJson '/api/reports/operations' $recH
Log $(if ($n7.status -eq 200) { 'OK' } else { 'FAIL' }) 'recepcion operations -> 200' "status=$($n7.status)"
$n8 = GetJson '/api/reports/low-stock' $vetH
Log $(if ($n8.status -eq 200) { 'OK' } else { 'FAIL' }) 'vet low-stock -> 200' "status=$($n8.status)"
$n9 = GetJson '/api/reports/low-stock' $recH
Log $(if ($n9.status -eq 403) { 'EXPECTED' } else { 'FAIL' }) 'recepcion low-stock -> 403' "status=$($n9.status)"

# =========================================================
# F12 - RESUMEN
# =========================================================
Write-Host "`n===== RESULTADO ====="
$nOk = ($results | Where-Object { $_.Ok -eq 'OK' }).Count
$nExp = ($results | Where-Object { $_.Ok -eq 'EXPECTED' }).Count
$nFail = ($results | Where-Object { $_.Ok -eq 'FAIL' }).Count
Write-Host "OK=$nOk  EXPECTED=$nExp  FAIL=$nFail"
if ($nFail -gt 0) {
    Write-Host "`n== FALLOS ==" -ForegroundColor Red
    $results | Where-Object { $_.Ok -eq 'FAIL' } | Format-Table -AutoSize | Out-String | Write-Host
} else {
    Write-Host "Sin fallos." -ForegroundColor Green
}
$results | Where-Object { $_.Ok -eq 'OK' -or $_.Ok -eq 'EXPECTED' } | Format-Table -AutoSize | Out-String | Write-Host

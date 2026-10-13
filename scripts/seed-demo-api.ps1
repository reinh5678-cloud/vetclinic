# Seed demo data through the live API to verify end-to-end flows.
# Requires: API running on http://localhost:8080 and seeded roles/users (admin/Admin123!).
$ErrorActionPreference = 'Stop'
$base = 'http://localhost:8080'
$results = @()
$script:h = @{}

function Log([string]$ok, [string]$what, $detail = '') {
    $script:results += [pscustomobject]@{ Ok = $ok; What = $what; Detail = [string]$detail }
    $mark = if ($ok -eq 'OK') { '  [OK]' } elseif ($ok -eq 'EXPECTED') { '  [EXP]' } else { ' [FAIL]' }
    Write-Host "$mark $what $detail"
}

function CallRequest([string]$method, [string]$path, $body = $null) {
    try {
        $params = @{ Uri = "$base$path"; Method = $method; Headers = $script:h }
        if ($null -ne $body) { $params.ContentType = 'application/json'; $params.Body = ($body | ConvertTo-Json -Depth 6) }
        $r = Invoke-RestMethod @params
        return @{ ok = $true; status = 200; data = $r }
    } catch {
        $status = 0
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) { $status = [int]$_.Exception.Response.StatusCode }
        $msg = $_.ErrorDetails.Message
        return @{ ok = $false; status = $status; error = $msg }
    }
}
function Post([string]$path, $body) { return CallRequest 'POST' $path $body }
function GetJson([string]$path) { return CallRequest 'GET' $path $null }
function Patch([string]$path, $body) { return CallRequest 'PATCH' $path $body }

# ---------- login ----------
$login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -ContentType 'application/json' -Body (@{ username = 'admin'; password = 'Admin123!' } | ConvertTo-Json)
$script:h = @{ Authorization = "Bearer $($login.token)" }
Log 'OK' "Login admin (permisos: $($login.permissions.Count))"

# ---------- users: find vet ----------
$users = (GetJson '/api/auth/users').data
$vetId = ($users | Where-Object { $_.username -eq 'vet' }).id
Log 'OK' "Vet user id=$vetId"

# ---------- tutors ----------
$tutors = @(
    @{ FirstName = 'Marcela';   LastName = 'Torres';    Email = 'marcela@example.com';   Phone = '555-0300'; Address = 'Calle Robles 12';    DocumentNumber = '23456789C' },
    @{ FirstName = 'Jorge';     LastName = 'Ramírez';   Email = 'jorge@example.com';     Phone = '555-0400'; Address = 'Av. Siempre Viva 88'; DocumentNumber = '34567890D' },
    @{ FirstName = 'Lucía';     LastName = 'Fernández'; Email = 'lucia@example.com';     Phone = '555-0500'; Address = 'Pasaje Los Pinos 3';   DocumentNumber = '45678901E' },
    @{ FirstName = 'Roberto';   LastName = 'Sánchez';   Email = 'roberto@example.com';   Phone = '555-0600'; Address = 'Jr. Los Olivos 21';   DocumentNumber = '56789012F' },
    @{ FirstName = 'Valentina'; LastName = 'Cruz';      Email = 'valentina@example.com'; Phone = '555-0700'; Address = 'Mza. B Lote 9';       DocumentNumber = '67890123G' }
)
$tMap = @{ Carlos = 1; Ana = 2 }
foreach ($t in $tutors) {
    $r = Post '/api/patients/tutors' $t
    if ($r.ok) { $tMap[$t.FirstName] = $r.data.id; Log 'OK' "Tutor $($t.FirstName) $($t.LastName)" "id=$($r.data.id)" }
    else { Log 'FAIL' "Tutor $($t.FirstName)" "$($r.status) $($r.error)" }
}

# ---------- pets ----------
$pets = @(
    @{ Name='Bruno'; Species='Perro'; Breed='Bulldog Francés';  BirthDate=(Get-Date).AddYears(-5).ToString('yyyy-MM-dd'); Sex='Macho'; WeightKg=12.3; Color='Atigrado'; TutorId=$tMap['Marcela']; IsSterilized=$true },
    @{ Name='Luna';  Species='Gato';  Breed='Persa';            BirthDate=(Get-Date).AddYears(-1).AddMonths(-6).ToString('yyyy-MM-dd'); Sex='Hembra'; WeightKg=3.8; Color='Blanco'; TutorId=$tMap['Ana']; IsSterilized=$false },
    @{ Name='Toby';  Species='Perro'; Breed='Golden Retriever'; BirthDate=(Get-Date).AddYears(-4).ToString('yyyy-MM-dd'); Sex='Macho'; WeightKg=30.2; Color='Dorado'; TutorId=$tMap['Marcela']; IsSterilized=$true },
    @{ Name='Coco';  Species='Ave';   Breed='Loro';            BirthDate=$null; Sex='Macho'; WeightKg=0.4; Color='Verde'; TutorId=$tMap['Marcela']; IsSterilized=$false; PhotoUrl='https://example.com/coco.jpg' },
    @{ Name='Max';   Species='Perro'; Breed='Pastor Alemán';   BirthDate=(Get-Date).AddYears(-6).ToString('yyyy-MM-dd'); Sex='Macho'; WeightKg=35.0; Color='Negro'; TutorId=$tMap['Jorge']; IsSterilized=$false },
    @{ Name='Nala';  Species='Gato';  Breed='Maine Coon';      BirthDate=(Get-Date).AddYears(-3).ToString('yyyy-MM-dd'); Sex='Hembra'; WeightKg=6.5; Color='Marrón'; TutorId=$tMap['Lucía']; IsSterilized=$true },
    @{ Name='Simba'; Species='Perro'; Breed='Maltés';          BirthDate=(Get-Date).AddYears(-1).ToString('yyyy-MM-dd'); Sex='Macho'; WeightKg=4.1; Color='Blanco'; TutorId=$tMap['Roberto']; IsSterilized=$false },
    @{ Name='Kiara'; Species='Gato';  Breed='Bengalí';         BirthDate=(Get-Date).AddYears(-2).ToString('yyyy-MM-dd'); Sex='Hembra'; WeightKg=4.7; Color='Manchado'; TutorId=$tMap['Valentina']; IsSterilized=$true }
)
$pMap = @{}
foreach ($p in $pets) {
    $body = $p.Clone()
    if (-not $body.BirthDate) { $body.Remove('BirthDate') }
    $r = Post '/api/patients/pets' $body
    if ($r.ok) { $pMap[$p.Name] = $r.data.id; Log 'OK' "Pet $($p.Name)" "id=$($r.data.id)" }
    else { Log 'FAIL' "Pet $($p.Name)" "$($r.status) $($r.error)" }
}

# ---------- inventory items ----------
$items = @(
    @{ Name='Vacuna Polivalente Felina'; Category='Vacunas';          Code='VAC-002'; Unit='dosis';  MinStock=8;  UnitPrice=30.0; Supplier='BioVet';     IsFractionable=$false },
    @{ Name='Vacuna Parvovirosis';       Category='Vacunas';          Code='VAC-003'; Unit='dosis';  MinStock=5;  UnitPrice=22.0; Supplier='BioVet';     IsFractionable=$false },
    @{ Name='Ivermectina 1%';            Category='Antiparasitarios'; Code='MED-002'; Unit='ml';     MinStock=50; UnitPrice=0.8;  Supplier='FarmaVet';  IsFractionable=$true },
    @{ Name='Metronidazol 125mg';        Category='Antibióticos';     Code='MED-003'; Unit='tab';    MinStock=10; UnitPrice=4.2;  Supplier='FarmaVet';  IsFractionable=$false },
    @{ Name='Ketoprofeno 10%';           Category='Antiinflamatorios';Code='MED-004'; Unit='ml';     MinStock=10; UnitPrice=18.0; Supplier='FarmVet';   IsFractionable=$true },
    @{ Name='Suero Fisiológico 500ml';   Category='Insumos';          Code='INS-002'; Unit='sachet'; MinStock=10; UnitPrice=6.5;  Supplier='MediSupply'; IsFractionable=$false },
    @{ Name='Pipeta Antipulgas';         Category='Parasiticidas';    Code='INS-003'; Unit='dosis';  MinStock=15; UnitPrice=45.0; Supplier='VetGuard';  IsFractionable=$false },
    @{ Name='Croquetas Adulto 3kg';      Category='Alimentos';        Code='ALI-001'; Unit='bolsa';  MinStock=5;  UnitPrice=350.0; Supplier='NutriPet';  IsFractionable=$false }
)
$iMap = @{}
foreach ($i in $items) {
    $r = Post '/api/inventory' $i
    if ($r.ok) { $iMap[$i.Code] = $r.data.id; Log 'OK' "Item $($i.Code) $($i.Name)" "id=$($r.data.id)" }
    else { Log 'FAIL' "Item $($i.Code)" "$($r.status) $($r.error)" }
}
# existing seeded items (años previos del seed)
$iMap['VAC-001'] = 1; $iMap['MED-001'] = 2; $iMap['INS-001'] = 3

# ---------- lots (con vencimientos conocidos para FEFO/expiring-soon) ----------
$lots = @(
    @{ Code='VAC-002'; Lot='LOT-VAC2-A'; Exp=45;  Qty=15 },
    @{ Code='VAC-002'; Lot='LOT-VAC2-B'; Exp=20;  Qty=10 },
    @{ Code='VAC-003'; Lot='LOT-VAC3-A'; Exp=90;  Qty=20 },
    @{ Code='MED-002'; Lot='LOT-IVM-0';  Exp=-10; Qty=50 },
    @{ Code='MED-002'; Lot='LOT-IVM-1';  Exp=300; Qty=200 },
    @{ Code='MED-002'; Lot='LOT-IVM-2';  Exp=35;  Qty=300 },
    @{ Code='MED-003'; Lot='LOT-MET-1';  Exp=400; Qty=20 },
    @{ Code='MED-004'; Lot='LOT-KET-1';  Exp=60;  Qty=20 },
    @{ Code='INS-002'; Lot='LOT-SUERO-1'; Exp=365; Qty=30 },
    @{ Code='INS-003'; Lot='LOT-PIP-1';  Exp=750; Qty=40 },
    @{ Code='ALI-001'; Lot='LOT-CROQ-1'; Exp=120; Qty=10 },
    @{ Code='MED-001'; Lot='LOT-AMOX-1'; Exp=200; Qty=40 }
)
foreach ($l in $lots) {
    $body = @{ LotNumber = $l.Lot; ExpirationDate = (Get-Date).AddDays($l.Exp).ToString('yyyy-MM-dd'); Quantity = $l.Qty; Description = "Lote de pruebas API ($($l.Code))" }
    $r = Post "/api/inventory/$($iMap[$l.Code])/lots" $body
    if ($r.ok) { Log 'OK' "Lote $($l.Lot)" "id=$($r.data.id)" }
    else { Log 'FAIL' "Lote $($l.Lot)" "$($r.status) $($r.error)" }
}

# ---------- stock adjustments (Kardex) ----------
$a1 = Patch "/api/inventory/$($iMap['ALI-001'])/stock" @{ Quantity = 5; Description = 'Compra directa sin lote' }
if ($a1.ok) { Log 'OK' 'PATCH stock +5 ALI-001' "stock=$($a1.data.stockQuantity)" } else { Log 'FAIL' 'PATCH stock +5 ALI-001' "$($a1.status) $($a1.error)" }
$a2 = Patch "/api/inventory/$($iMap['MED-003'])/stock" @{ Quantity = -3; Description = 'Uso interno en consulta' }
if ($a2.ok) { Log 'OK' 'PATCH stock -3 MED-003' "stock=$($a2.data.stockQuantity)" } else { Log 'FAIL' 'PATCH stock -3 MED-003' "$($a2.status) $($a2.error)" }

# ---------- appointments ----------
$appts = @(
    @{ Start=(Get-Date).AddDays(1).Date.AddHours(10); Dur=30; Urg='Media';      Reason='Control post-vacunación';        Pet='Bruno' },
    @{ Start=(Get-Date).AddDays(2).Date.AddHours(11); Dur=45; Urg='Baja';       Reason='Revisión anual senior';           Pet='Max' },
    @{ Start=(Get-Date).AddDays(3).Date.AddHours(9);  Dur=30; Urg='Alta';       Reason='Diarrea persistente';             Pet='Simba' },
    @{ Start=(Get-Date).AddDays(1).Date.AddHours(16); Dur=30; Urg='Emergencia'; Reason='Ingestión de objeto extraño';     Pet='Toby' },
    @{ Start=(Get-Date).AddDays(5).Date.AddHours(12); Dur=30; Urg='Media';      Reason='Vacuna polivalente';              Pet='Luna' }
)
$apptMap = @{}
foreach ($a in $appts) {
    $body = @{ StartTime = $a.Start.ToString('yyyy-MM-ddTHH:mm:ss'); DurationMinutes = $a.Dur; Urgency = $a.Urg; Reason = $a.Reason; PetId = $pMap[$a.Pet]; VetId = $vetId }
    $r = Post '/api/appointments' $body
    if ($r.ok) { $apptMap[$a.Pet] = $r.data.id; Log 'OK' "Cita $($a.Pet) ($($a.Urg))" "id=$($r.data.id)" }
    else { Log 'FAIL' "Cita $($a.Pet)" "$($r.status) $($r.error)" }
}
# transiciones de estado
$s1 = Patch "/api/appointments/$($apptMap['Toby'])/status" @{ Status='EnConsulta'; Notes='En evaluación' }
if ($s1.ok) { Log 'OK' 'Cita Toby -> EnConsulta' } else { Log 'FAIL' 'Cita Toby -> EnConsulta' "$($s1.status) $($s1.error)" }
$s2 = Patch "/api/appointments/$($apptMap['Toby'])/status" @{ Status='Completada'; Notes='Cuerpo extraño extraído sin complicaciones' }
if ($s2.ok) { Log 'OK' 'Cita Toby -> Completada' } else { Log 'FAIL' 'Cita Toby -> Completada' "$($s2.status) $($s2.error)" }
$s3 = Patch "/api/appointments/$($apptMap['Max'])/status" @{ Status='Cancelada'; Notes='Tutor no disponible' }
if ($s3.ok) { Log 'OK' 'Cita Max -> Cancelada' } else { Log 'FAIL' 'Cita Max -> Cancelada' "$($s3.status) $($s3.error)" }
$s4 = Patch "/api/appointments/$($apptMap['Simba'])/status" @{ Status='NoAsistio'; Notes='No se presentó' }
if ($s4.ok) { Log 'OK' 'Cita Simba -> NoAsistio' } else { Log 'FAIL' 'Cita Simba -> NoAsistio' "$($s4.status) $($s4.error)" }

# ---------- medical records + prescripciones + lab ----------
$mr1 = Post '/api/medical-records' @{ PetId=$pMap['Bruno']; VetId=$vetId; Symptoms='Decaimiento y tos'; Findings='RU transitorios, fiebre 39.6'; Diagnosis='Traqueobronquitis leve'; TreatmentPlan='Reposo y amoxicilina 7 días'; WeightKg=12.3; TemperatureC=39.6 }
if ($mr1.ok) {
    Log 'OK' 'Registro médico Bruno' "id=$($mr1.data.id)"
    $pr = Post "/api/medical-records/$($mr1.data.id)/prescriptions" @{ MedicationName='Amoxicilina 250mg'; Dosage='1 tab cada 12h'; Frequency='cada 12 horas'; Duration='7 días'; Instructions='Administrar con comida' }
    if ($pr.ok) { Log 'OK' 'Prescripción Bruno' } else { Log 'FAIL' 'Prescripción Bruno' "$($pr.status) $($pr.error)" }
} else { Log 'FAIL' 'Registro médico Bruno' "$($mr1.status) $($mr1.error)" }

$mr2 = Post '/api/medical-records' @{ PetId=$pMap['Simba']; VetId=$vetId; Symptoms='Diarrea líquida, inapetencia'; Findings='Mucosas pálidas, deshidratación'; Diagnosis='Gastroenteritis'; TreatmentPlan='Fluidoterapia + metronidazol'; WeightKg=4.1; TemperatureC=38.7 }
if ($mr2.ok) {
    Log 'OK' 'Registro médico Simba' "id=$($mr2.data.id)"
    $lb = Post "/api/medical-records/$($mr2.data.id)/lab-results" @{ TestType='Hematología'; Result='Leucocitos 22.000/µL'; ReferenceRange='6.000-17.000/µL'; IsAbnormal=$true }
    if ($lb.ok) { Log 'OK' 'Lab anormal Simba' } else { Log 'FAIL' 'Lab anormal Simba' "$($lb.status) $($lb.error)" }
    $ln = Post "/api/medical-records/$($mr2.data.id)/lab-results" @{ TestType='Perfil renal'; Result='Creatinina 1.0 mg/dL'; ReferenceRange='0.5-1.6 mg/dL'; IsAbnormal=$false }
    if ($ln.ok) { Log 'OK' 'Lab normal Simba' } else { Log 'FAIL' 'Lab normal Simba' "$($ln.status) $($ln.error)" }
} else { Log 'FAIL' 'Registro médico Simba' "$($mr2.status) $($mr2.error)" }

$mr3 = Post '/api/medical-records' @{ PetId=$pMap['Mia']; VetId=$vetId; Symptoms='Estornudos y secreción nasal'; Findings='Leve conjuntivitis'; Diagnosis='Rinitis felina'; TreatmentPlan='Limpieza ocular + observación' }
if ($mr3.ok) { Log 'OK' 'Registro médico Mia' "id=$($mr3.data.id)" } else { Log 'FAIL' 'Registro médico Mia' "$($mr3.status) $($mr3.error)" }

# ---------- vacunas ----------
$v1 = Post "/api/patients/pets/$($pMap['Bruno'])/vaccines" @{ InventoryItemId=$iMap['VAC-002']; ApplicationDate=(Get-Date).AddDays(-1).ToString('yyyy-MM-dd'); NextDueDate=(Get-Date).AddMonths(12).ToString('yyyy-MM-dd') }
if ($v1.ok) { Log 'OK' 'Vacuna Bruno' } else { Log 'FAIL' 'Vacuna Bruno' "$($v1.status) $($v1.error)" }
$v2 = Post "/api/patients/pets/$($pMap['Luna'])/vaccines" @{ InventoryItemId=$iMap['VAC-002']; ApplicationDate=(Get-Date).AddDays(-5).ToString('yyyy-MM-dd'); NextDueDate=(Get-Date).AddDays(25).ToString('yyyy-MM-dd') }
if ($v2.ok) { Log 'OK' 'Vacuna Luna (due soon)' "isDueSoon=$($v2.data.isDueSoon)" } else { Log 'FAIL' 'Vacuna Luna' "$($v2.status) $($v2.error)" }
$v3 = Post "/api/patients/pets/$($pMap['Coco'])/vaccines" @{ InventoryItemId=$iMap['VAC-003']; ApplicationDate=(Get-Date).ToString('yyyy-MM-dd'); NextDueDate=$null }
if ($v3.ok) { Log 'OK' 'Vacuna Coco' } else { Log 'FAIL' 'Vacuna Coco' "$($v3.status) $($v3.error)" }

# ---------- hospitalizaciones ----------
$h1 = Post '/api/hospitalizations' @{ PetId=$pMap['Nala']; AssignedVetId=$vetId; Reason='Vómitos y letargo'; Notes='Requiere fluidoterapia' }
if ($h1.ok) {
    Log 'OK' 'Hospitalización Nala (activa)' "id=$($h1.data.id)"
    $e1 = Post "/api/hospitalizations/$($h1.data.id)/evolutions" @{ Temperature=38.9; HeartRate=95; RespiratoryRate=28; Observations='Estable, hidratación en curso' }
    $e2 = Post "/api/hospitalizations/$($h1.data.id)/evolutions" @{ Temperature=38.6; HeartRate=88; RespiratoryRate=24; Observations='Mejoría, tolera alimento' }
    $m1 = Post "/api/hospitalizations/$($h1.data.id)/medications" @{ MedicationName='Metoclopramida 5mg'; Dose='0.5 mg/kg'; Route='IV'; AdministeredAt=(Get-Date).ToString('yyyy-MM-ddTHH:mm:ss') }
    if ($e1.ok -and $e2.ok -and $m1.ok) { Log 'OK' 'Evoluciones + medicación Nala' } else { Log 'FAIL' 'Evoluciones + medicación Nala' 'revisar' }
} else { Log 'FAIL' 'Hospitalización Nala' "$($h1.status) $($h1.error)" }

$h2 = Post '/api/hospitalizations' @{ PetId=$pMap['Toby']; AssignedVetId=$vetId; Reason='Observación post procedimiento'; Notes='Inicio de alta' }
if ($h2.ok) {
    $e3 = Post "/api/hospitalizations/$($h2.data.id)/evolutions" @{ Temperature=38.4; HeartRate=80; RespiratoryRate=20; Observations='Recuperación sin novedades' }
    $d1 = Post "/api/hospitalizations/$($h2.data.id)/discharge" @{}
    if ($d1.ok) { Log 'OK' 'Alta Toby' "status=$($d1.data.status)" } else { Log 'FAIL' 'Alta Toby' "$($d1.status) $($d1.error)" }
} else { Log 'FAIL' 'Hospitalización Toby' "$($h2.status) $($h2.error)" }

# ---------- facturas ----------
$f1 = Post '/api/invoices' @{ TutorId=$tMap['Marcela']; PetId=$pMap['Bruno']; Status='Pagada'; PaymentMethod='Tarjeta'; Lines=@(
    @{ Description='Consulta general'; Quantity=1; UnitPrice=800; InventoryItemId=$null },
    @{ Description='Amoxicilina 250mg'; Quantity=10; UnitPrice=5.5; InventoryItemId=$iMap['MED-001'] },
    @{ Description='Vacuna Polivalente Felina'; Quantity=1; UnitPrice=30; InventoryItemId=$iMap['VAC-002'] }
) }
if ($f1.ok) { Log 'OK' 'Factura F1 (Pagada/Tarjeta + stock)' "subtotal=$($f1.data.subtotal) taxes=$($f1.data.taxes) total=$($f1.data.total) #$($f1.data.invoiceNumber)" }
else { Log 'FAIL' 'Factura F1' "$($f1.status) $($f1.error)" }

$f2 = Post '/api/invoices' @{ TutorId=$tMap['Ana']; PetId=$pMap['Mia']; Status='Emitida'; PaymentMethod='Efectivo'; Lines=@(
    @{ Description='Ivermectina 1%'; Quantity=2.5; UnitPrice=0.8; InventoryItemId=$iMap['MED-002'] }
) }
if ($f2.ok) {
    $expTax = [math]::Round([decimal]$f2.data.subtotal * 0.16, 2)
    $okTax = ([decimal]$f2.data.taxes -eq $expTax)
    Log $(if ($okTax) { 'OK' } else { 'FAIL' }) 'Factura F2 fracción 2.5ml' "subtotal=$($f2.data.subtotal) taxes=$($f2.data.taxes) esperado=$expTax"
} else { Log 'FAIL' 'Factura F2' "$($f2.status) $($f2.error)" }

$f3 = Post '/api/invoices' @{ TutorId=$tMap['Jorge']; PetId=$pMap['Max']; Status='Borrador'; PaymentMethod='Transferencia'; Lines=@(
    @{ Description='Suero Fisiológico 500ml'; Quantity=2; UnitPrice=6.5; InventoryItemId=$iMap['INS-002'] }
) }
if ($f3.ok) { Log 'OK' 'Factura F3 (Borrador/Transferencia)' "total=$($f3.data.total)" } else { Log 'FAIL' 'Factura F3' "$($f3.status) $($f3.error)" }

# guardas esperadas (deben rechazarse)
$fx1 = Post '/api/invoices' @{ TutorId=$tMap['Roberto']; PetId=$pMap['Simba']; Status='Emitida'; PaymentMethod='Efectivo'; Lines=@(
    @{ Description='Ivermectina fraccionada en ítem no-fraccionable'; Quantity=1.5; UnitPrice=30; InventoryItemId=$iMap['VAC-002'] }
) }
if ($fx1.ok) { Log 'FAIL' 'F4 rechazo fracción en no-fraccionable' 'se creó cuando debía rechazarse' } else { Log 'EXPECTED' 'F4 rechazo fracción no-fraccionable' "$($fx1.status) $($fx1.error)" }

$fx2 = Post '/api/invoices' @{ TutorId=$tMap['Roberto']; PetId=$pMap['Simba']; Status='Emitida'; PaymentMethod='Cripto'; Lines=@(
    @{ Description='Línea inválida'; Quantity=1; UnitPrice=10; InventoryItemId=$null }
) }
if ($fx2.ok) { Log 'FAIL' 'F5 rechazo medio de pago inválido' 'se creó cuando debía rechazarse' } else { Log 'EXPECTED' 'F5 rechazo medio de pago inválido' "$($fx2.status) $($fx2.error)" }

# FEFO: F2 (2.5ml) debió consumir primero el lote vencido LOT-IVM-0
$lots2 = (GetJson "/api/inventory/$($iMap['MED-002'])/lots").data
$expiredConsumed = -not ($lots2 | Where-Object { $_.lotNumber -eq 'LOT-IVM-0' -and $_.availableQuantity -gt 0 })
Log $(if ($expiredConsumed) { 'OK' } else { 'FAIL' }) 'FEFO' 'lote vencido consumido primero (MED-002)'

# lotes próximos a vencer -> IsExpiringSoon
$lotsV2 = (GetJson "/api/inventory/$($iMap['VAC-002'])/lots").data
$soon = @($lotsV2 | Where-Object { $_.isExpiringSoon }).Count
Log $(if ($soon -eq 1) { 'OK' } else { 'FAIL' }) 'ExpiringSoon VAC-002' "marcados=$soon (esperado 1)"

# ---------- reportes ----------
$dash = (GetJson '/api/reports/dashboard?from=2026-01-01&to=2026-12-31').data
if ($dash) { Log 'OK' 'Dashboard reports' "invocó OK" } else { Log 'FAIL' 'Dashboard reports' 'sin datos' }
$rev = (GetJson '/api/reports/revenues').data
if ($rev) { Log 'OK' 'Revenues reports' 'invocó OK' } else { Log 'FAIL' 'Revenues reports' 'sin datos' }
$ret = (GetJson '/api/reports/retention').data
if ($ret) { Log 'OK' 'Retention reports' 'invocó OK' } else { Log 'FAIL' 'Retention reports' 'sin datos' }

# ---------- resumen ----------
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
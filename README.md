# VetClinic — Sistema de Gestión de Clínica Veterinaria

Sistema **monopuesto** (un solo equipo) para gestionar una clínica veterinaria: pacientes, agenda, historia clínica digital, hospitalización, inventario/farmacia, facturación y reportes KPI, con control de acceso por roles.

Proyecto demostrativo con arquitectura por capas y propósitos de aprendizaje.

## Stack

| Capa | Tecnología |
|---|---|
| Backend | **ASP.NET Core 8** (Web API) |
| ORM / BD | **EF Core 8 + PostgreSQL 16** |
| Auth | **JWT Bearer** (roles por claim) |
| Frontend | **React 19 + TypeScript + Vite** |
| Infra local | **Docker Compose** (PostgreSQL) |

## Estructura

```
vetclinic/
├─ backend/
│  ├─ VetClinic.sln
│  └─ src/
│     ├─ VetClinic.Domain/         # Entidades y enums (16 entidades)
│     ├─ VetClinic.Application/    # DTOs, interfaces y servicios (lógica de negocio)
│     ├─ VetClinic.Infrastructure/ # EF Core DbContext, repositorios, seed, migraciones
│     └─ VetClinic.Api/            # Controladores REST, autenticación JWT, Swagger
├─ frontend/                       # SPA React (Vite), proxy /api → :8080
│   Pantallas: Login, Dashboard, Pacientes, Agenda, Historia Clínica,
│   Hospitalización, Inventario, Facturación, Configuración
├─ docker-compose.yml              # PostgreSQL 16
└─ docs/
```

### Módulos funcionales (endpoints)

| Módulo | Base | Roles |
|---|---|---|
| Autenticación | `/api/auth` | público |
| Pacientes y tutores | `/api/patients` | `patients.view` (lectura) / `patients.manage` (escritura) |
| Agenda / citas | `/api/appointments` | todos (valida cruce de horarios) |
| Historia clínica | `/api/medical-records` | Veterinario, Administrador |
| Hospitalización | `/api/hospitalizations` | Veterinario, Administrador |
| Inventario / farmacia | `/api/inventory` | Veterinario, Administrador |
| Facturación | `/api/invoices` | todos |
| Reportes KPI | `/api/reports` | métricas financieras: Administrador; operativas (`operations`): todos; `low-stock`: Administrador/Veterinario |
| Configuración | `/api/settings` | Administrador |

## Puesta en marcha

Requisitos: **.NET 8 SDK**, **Node 20+**, **Docker Desktop** (para PostgreSQL).

```bash
# 1) Base de datos (PostgreSQL en Docker)
docker compose up -d postgres

# 2) Configuración local (secretos NO versionados)
#    Copia la plantilla y ajusta si es necesario
cp backend/src/VetClinic.Api/appsettings.local.example.json backend/src/VetClinic.Api/appsettings.local.json

# 3) Backend (aplica migraciones + seed automáticamente)
dotnet run --project backend/src/VetClinic.Api --urls http://localhost:8080
# Swagger: http://localhost:8080/swagger

# 4) Frontend
cd frontend
npm install
npm run dev
# App: http://localhost:5173
```

> **Secretos**: `Jwt:Key` y la cadena de conexión ya no se versionan. Se leen de `appsettings.local.json` (gitignored) o, alternativamente, de variables de entorno: `Jwt__Key` y `ConnectionStrings__DefaultConnection` (usa `__` para anidar). Para la BD, `docker compose` toma `POSTGRES_PASSWORD` desde `.env` (ver `.env.example`). El backend falla al arrancar si `Jwt:Key` falta o tiene menos de 32 caracteres.

### Usuarios demo (seed)

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin` | `Admin123!` | Administrador (reportes) |
| `vet` | `Vet123!` | Veterinario (clínico, inventario) |
| `recepcion` | `Recep123!` | Recepción (agenda, pacientes, facturas) |

## Capas y flujo de datos

```
React (SPA)  ──HTTP──>  VetClinic.Api (controllers + JWT)
                          │
                          ▼
                     Application (servicios/DTOs)
                          │
                          ▼
                  Infrastructure (EF Core → PostgreSQL)
```

- **Domain**: entidades puras sin dependencias (Paciente, Cita, HistoriaClínica, Inventario, Factura, etc.).
- **Application**: contratos, DTOs y servicios. Reglas de negocio (descuento de stock, IVA 16%, solapamiento de citas, urgencias).
- **Infrastructure**: `VetClinicDbContext`, `Repository<T>` genérico, seed, migración `InitialCreate`.
- **Api**: controladores, `[Authorize(Roles = "...")]` y políticas de permiso (`perm:<código>`), Swagger, CORS, migraciones auto-aplicadas al arrancar.

## Roles y permisos (matching con el contexto)

- **Veterinarios**: lectura/escritura en lo clínico (historias, recetas, hospitalización) e inventario.
- **Recepción**: agenda, pacientes, facturación básica; sin acceso a diagnosticos/expedientes médicos (no tiene rol `Veterinario`).
- **Administradores**: todo, incluidos `/api/reports` (ingresos, retención, stock bajo).

## Dashboard (panel por rol)

La pantalla `/` es el **Dashboard** y es visible para **todos los roles autenticados**; cada uno ve las secciones que le corresponden por permisos:

- **Operativo** (`GET /api/reports/operations`, cualquier autenticado): citas de hoy, pendientes, en espera/consulta, urgencias abiertas, hospitalizados activos, vacunas próximas (30 días), recordatorios pendientes y facturas sin cobrar.
- **Financiero** (`GET /api/reports/dashboard|revenues|retention`, solo Administrador): ingresos del período con variación vs período anterior, facturas, pacientes, clientes, citas programadas, tendencia de ingresos (últimos 12 meses) y retención de clientes.
- **Stock bajo** (`GET /api/reports/low-stock`, Administrador/Veterinario): tabla de artículos bajo el mínimo.

El Administrador puede elegir el período (Hoy / 7 días / 30 días / Este mes / Este año / personalizado). Los indicadores financieros respetan el período; la tendencia muestra siempre los últimos 12 meses.

## Migraciones

```bash
cd backend
dotnet ef migrations add NombreCambio \
  --project src/VetClinic.Infrastructure \
  --startup-project src/VetClinic.Api
```

## Concurrencia optimista

Las operaciones de inventario y facturación son de lectura-modificación-escritura (stock, lotes, estado de factura) y pueden sufrir *lost updates* si dos usuarios/peticiones actúan a la vez. Se usa el token de concurrencia nativo de PostgreSQL: la columna de sistema **`xmin`**, configurada como `Property<uint>("xmin").IsRowVersion()` en `VetClinicDbContext`, aplicada a `InventoryItem`, `ProductLot` e `Invoice`.

- **Sin cambios de esquema**: `xmin` ya existe en todas las tablas; no se agrega ninguna columna. La migración `ConcurrencyTokens` es **intencionalmente vacía** (solo documenta la incorporación del token).
- EF agrega `WHERE xmin = @original` a cada `UPDATE`. Si la fila cambió desde que se leyó, no se actualiza ninguna fila y EF lanza `DbUpdateConcurrencyException`.
- `ApiExceptionMiddleware` la traduce a **HTTP 409 Conflict** con el mensaje: «El registro fue modificado por otra operación. Recargá los datos e intentá de nuevo.»
- El frontend debe mostrar ese `409`, recargar los datos y ofrecer reintentar (no hay reintento automático).

Verificado con 20 ajustes de stock concurrentes sobre el mismo producto: 2 respuestas `200` y 18 `409`; el stock final refleja únicamente las operaciones confirmadas (sin updates perdidos). Hay una prueba de modelo en `backend/tests/VetClinic.UnitTests/ConcurrencyModelTests.cs`.

## Configuración (administración)

Pantalla **solo para el rol `Administrador`** (`/configuracion` en la SPA; `[Authorize(Roles = "Administrador")]` en el backend). El acceso se controla por **rol**, no por un permiso nuevo, para no requerir cambios de seed sobre bases existentes. La pantalla se organiza en tres subpestañas: **Usuarios**, **Roles** y **Parámetros**.

### Usuarios

- **Listar**: `GET /api/settings/users` (todos, activos e inactivos).
- **Crear**: `POST /api/settings/users` (usuario, contraseña mín. 6, nombre, email, rol, PIN opcional).
- **Editar** (incluye **asignar rol**): `PUT /api/settings/users/{id}` (nombre, email, rol, activo).
- **Resetear contraseña**: `POST /api/settings/users/{id}/password`.
- **Gestionar PIN**: `POST /api/settings/users/{id}/pin` (`null`/vacío lo quita).

**Protecciones**: un administrador no puede desactivar su propio usuario ni cambiar su propio rol (`409 Conflict`); `Username` y `PinAccess` son únicos (duplicado → `409`). PIN y contraseñas se guardan con `BCrypt`.

### Roles

- **Listar** (con permisos y cantidad de usuarios): `GET /api/settings/roles`.
- **Detalle**: `GET /api/settings/roles/{id}`.
- **Crear**: `POST /api/settings/roles` (`name` + `permissions` del catálogo).
- **Editar**: `PUT /api/settings/roles/{id}` (nombre y permisos).
- **Catálogo de permisos**: `GET /api/settings/permissions` (código, grupo y etiqueta legible para la UI).

Los permisos se validan contra el catálogo (`Permissions.All`); un permiso desconocido devuelve `400`. El nombre es obligatorio, único (case-insensitive) y de hasta 50 caracteres. El rol **`Administrador` es del sistema**: no puede renombrarse ni editarse (`409`), para evitar que la instalación quede sin acceso a Configuración.

### Parámetros del sistema

- **Lectura** (`GET /api/settings/system`): IVA, expiración de sesión, ruta de adjuntos, tamaño máx. de adjunto (20 MB), umbral «próximo a vencer» (30 días), entorno y versión. Provienen de configuración o de constantes del código.
- **IVA editable** (`PUT /api/settings/billing`): valida `0 ≤ TaxRate ≤ 1`, persiste `Billing:TaxRate` en `appsettings.local.json` (no versionado) y recarga la configuración en caliente (`IConfigurationRoot.Reload()`), sin reiniciar la API.

## Pacientes y tutores

Pantalla `/pacientes` (`patients.view` para consultar; `patients.manage` para crear/editar). Se organiza en tres subpestañas: **Pacientes**, **Tutores** y **Vacunas y recordatorios**.

### Pacientes
- **Listar / buscar**: `GET /api/patients/pets?search=&species=&active=` (filtra por nombre/raza/tutor, especie y estado; el orden —nombre/edad/peso— se aplica en el cliente).
- **Detalle (ficha)**: `GET /api/patients/pets/{id}`. La fila de la tabla se **expande** y muestra datos del paciente, sus vacunas, últimas consultas y citas.
- **Crear**: `POST /api/patients/pets` (nombre, especie, raza, fecha de nacimiento, sexo, peso, color, tutor, esterilizado y foto por URL).
- **Editar**: `PUT /api/patients/pets/{id}`.
- **Activar / desactivar** (baja lógica): `PUT /api/patients/pets/{id}/active`.

### Tutores
- **Listar** (con cantidad de mascotas): `GET /api/patients/tutors/list`.
- **Detalle**: `GET /api/patients/tutors/{id}`.
- **Crear / editar**: `POST /api/patients/tutors`, `PUT /api/patients/tutors/{id}`.
- Se conservan `GET /api/patients/tutors` (Tutor básico) y `GET /api/patients/tutors/{tutorId}/pets`, usados por Facturación y Agenda.

### Vacunas y recordatorios
- **Próximas dosis**: `GET /api/patients/vaccines/upcoming?days=30` (incluye vencidas y próximas; marca `isOverdue`).
- **Por paciente**: `GET/POST /api/patients/pets/{id}/vaccines`. Registrar una vacuna consume stock por lote (FEFO) y crea un recordatorio pasivo si hay próxima dosis.

**Validaciones** (`400`): nombre del paciente y nombre/apellido del tutor obligatorios; peso no negativo; el tutor asignado debe existir. **Autorización**: cada endpoint exige `patients.view` o `patients.manage` mediante la política `perm:<código>` (atributo `RequirePermission`); sin el permiso → `403`.

## Documentación

- `docs/architecture.md` — diagramas (flujo de trabajo, modelo relacional, arquitectura).
- `docs/development-log.md` — bitácora detallada: herramientas usadas, módulos, endpoints, reglas de negocio y cómo probar de punta a punta.

## Pendientes (backlog — continuar luego)

Estado: sistema funcional (backend completo + frontend con todas las pantallas). Esto es lo que queda por hacer:

~~1. **Carga real de adjuntos** — **Hecho**: upload multipart en `POST /api/medical-records/{recordId}/attachments` (máx. 20 MB) con descarga/visualización en `GET /api/medical-records/{recordId}/attachments/{attachmentId}/download`. Archivos en `backend/src/VetClinic.Api/uploads/attachments/`.~~
~~2. **Backups automáticos de la base de datos** — **Hecho**: scripts en `scripts/` (`backup-db.ps1` y `restore-db.ps1`) usando `pg_dump -Fc` + `pg_restore`; ver sección «Backups» abajo.~~
3. ~~Recordatorios reales (SMS/WhatsApp/Correo)~~ — **Descartado por decisión del equipo** por el momento.
4. ~~Multi-puesto / contingencia~~ — **Descartado por decisión del equipo** por el momento.

## Backups

Base de datos PostgreSQL corre en Docker (`vetclinic-postgres`). Los scripts asumen el contenedor, usuario y base del `docker-compose.yml`.

```powershell
# 1) Generar un backup (pg_dump formato custom, comprimido)
powershell -ExecutionPolicy Bypass -File scripts\backup-db.ps1
# >> backups\vetclinic_<fecha>.dump  (conserva los últimos 14 días)

# 2) Restaurar un backup (reemplaza el contenido actual de la BD)
powershell -ExecutionPolicy Bypass -File scripts\restore-db.ps1 -BackupFile backups\vetclinic_20260923_080602.dump
```

**Programar backup diario (Programador de tareas de Windows):**

```powershell
schtasks /create /tn "VetClinicBackup" /tr "powershell -ExecutionPolicy Bypass -File D:\vetclinic\scripts\backup-db.ps1" /sc daily /st 02:00
schtasks /run /tn "VetClinicBackup"   # probar ejecución
```

> Nota: la restauración usa `--clean --if-exists` sobre la base `vetclinic`; detén el backend (la API) antes de restaurar para evitar conexiones activas. La API vuelve a aplicar migraciones al arrancar.

> Nota: la API debe arrancar con `ASPNETCORE_ENVIRONMENT=Development` para habilitar Swagger en `http://localhost:8080/swagger`.

## Backlog — Evolución del modelo de datos (plan aprobado)

Plan para llevar la BD al diseño «núcleo completo» (roles/permisos, campos clínicos y vacunas, inventario con lotes + Kardex y ventas ACID). **Decisiones acordadas:**
- Se mantiene el stack actual (React + .NET API + PostgreSQL), sin cambio a escritorio/SQLite.
- Se conservan los nombres `Tutor`/`Pet` (no se renombra a `clientes`/`pacientes`).
- El claim JWT `role` conserva el mismo formato (nombre del rol) → no rompe `[Authorize(Roles=...)]` ni el frontend actual.
- **Una sola migración EF Core** al final de todo (no una por fase).
- Implementación **fase por fase, verificando cada una** (build + pruebas).

### Fase 1 — Roles, permisos y PIN — **Hecho**
- Nueva entidad `Role` (`Id, Name` único, `PermissionsJson`) y `User`: `Role` (enum) → `RoleId` FK; nuevo campo `PinAccess`.
- `AuthService`: claim `ClaimTypes.Role` = `Role.Name` (misma forma); `UserDto.Role` = nombre del rol.
- Nuevos endpoints: `POST /api/auth/login-pin` (quick-switch por PIN) y `GET /api/auth/permissions`.
- `SeedData`: sembrar 3 roles con permisos (Administrador = todo; Veterinario = clínico + inventario; Recepción = agenda/pacientes/facturación).
- Migración: backfill mapeando usuarios por valor previo del enum, luego eliminar columna antigua.
- Frontend: `RequireAuth`/`Layout` pasan de hardcodear `role` a consumir permisos de `/api/auth/permissions`; control de login por PIN.

### Fase 2 — Entidades y clínico — **Hecho**
- `Tutor` + `DocumentNumber` (índice único); `Pet` + `IsSterilized`, `PhotoUrl`; `MedicalRecord` + `WeightKg`, `TemperatureC` (nullable).
- Nueva `VaccineApplication` (`PetId` FK, `InventoryItemId` FK, `ApplicationDate`, `NextDueDate`). Si hay `NextDueDate` → crear `Reminder` pasivo (solo fila; sin envío, descartado).
- Endpoints: `POST/GET /api/patients/pets/{petId}/vaccines`.
- Frontend: peso/temperatura en Historia Clínica, esterilizado/foto en formulario de paciente, sección "Vacunas" con alerta de próxima dosis.
- (*Opcional, no acordado por defecto*): tablas maestras de especie/raza.

### Fase 3 — Inventario con lotes + Kardex + venta ACID *(crítico)*
Hoy **facturar no descuenta stock**. Cambios:
- `InventoryItem`: `StockQuantity` `int` → `decimal` (caché) + `IsFractionable`.
- Nueva `ProductLot` (`InventoryItemId` FK, `LotNumber`, `ExpirationDate`, `AvailableQuantity` decimal).
- `StockMovement`: `Quantity` → `decimal`; nueva FK `ProductLotId`; `StockMovementType` se amplía a `EntradaCompra, SalidaVenta, SalidaUsoInterno, Ajuste`.
- Nuevo `IUnitOfWork` (`BeginTransactionAsync()`).
- `InvoiceService.CreateAsync` → **transacción ACID**: insertar factura + líneas → por línea consumir lotes **FEFO** (vencimiento más próximo) → stock insuficiente = `throw` → **rollback** → registrar `StockMovement` (SalidaVenta) y actualizar caché.
- `InventoryService`: nuevo `AddLotAsync` (lote + `StockMovement` EntradaCompra); `UpdateStockAsync` → `Ajuste`.
- FKs de historial en `Restrict` + soft-delete (`IsActive`).
- Migración backfill: lote `INICIAL` por producto con el stock actual; convertir columnas a `decimal`.
- Frontend: inventario con registro de lotes, alertas de stock bajo y próximos a vencer; facturación muestra error "Stock insuficiente".

### Verificación por fase
1. `dotnet build` + ejecutar la migración contra una **copia scratch** de la BD antes de aplicar a la real.
2. Fase 3: crear producto + lote → factura de 2 líneas → verificar lote/caché/Kardex; prueba de **rollback** (stock insuficiente → factura no existe, lote intacto).
3. Frontend: `npm run lint` + `npm run build` + smoke test de los flujos tocados.

### Fuera de alcance (decisión del equipo)
Laboratorio estructurado, proveedores/órdenes de compra, jaulas/camas, peluquería/estética con comisiones, legal/firmas, y **envío** de recordatorios (solo se crea la fila `Reminder`).
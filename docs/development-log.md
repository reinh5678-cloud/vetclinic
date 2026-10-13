# Bitácora de desarrollo — VetClinic

Documento de referencia: qué se construyó, con qué herramientas, cómo funciona cada módulo y qué decisiones se tomaron.

Última actualización: 2026-10-12

---

## 1. Resumen del sistema

**VetClinic** es un sistema de gestión para clínica veterinaria monousuario (arquitectura **monopuesto**), construido como proyecto demostrativo/aprendizaje. Conecta las áreas clínica, administrativa y de atención al cliente en una sola aplicación web local con control de acceso por roles.

Hace las veces de software de historia clínica electrónica, agenda, control de hospitalización, inventario de farmacia, facturación y generación de reportes KPI.

---

## 2. Herramientas y tecnologías utilizadas

| Capa | Herramienta | Versión | Uso |
|---|---|---|---|
| Backend | **.NET SDK** | 8.0.423 | Plataforma de desarrollo C# |
| Backend framework | **ASP.NET Core 8** (Web API) | 8.0 | REST API, middleware, auth JWT |
| ORM | **Entity Framework Core 8** | 8.0.31 | Acceso a datos, migraciones, seed |
| Base de datos | **PostgreSQL** | 16 (Docker `postgres:16-alpine`) | Almacenamiento relacional |
| Migraciones | **dotnet-ef** | 8.0.31 | Generación/aplicación de migraciones |
| Contraseñas | **BCrypt.Net-Next** | 4.2.0 | Hash de contraseñas (nunca en texto plano) |
| Tokens | **JWT Bearer** + System.IdentityModel.Tokens.Jwt | 7.7.3 | Autenticación/autorización por roles |
| Conector BD | **Npgsql.EntityFrameworkCore.PostgreSQL** | 8.x | Driver PostgreSQL para EF Core |
| Frontend | **React 19 + TypeScript** | 19.x / 5.x | SPA y tipado estático |
| Build front | **Vite** | 8.3.0 | Dev server, proxy `/api`→:8080, build |
| Rutas SPA | **react-router-dom** | 7.x | Navegación (login, dashboard, módulos) |
| Orquestación | **Docker Compose** | v5.5.1 | PostgreSQL local (volumen persistente) |
| IDE | **Visual Studio Code** | — | Edición, tasks, launch (`C# Dev Kit`, Prettier) |
| Git | **git** | — | Control de versiones (aún por `git init`) |

---

## 3. Arquitectura (capas)

```
frontend/src ──HTTP /api──▶ VetClinic.Api ──▶ Application ──▶ Infrastructure ──▶ PostgreSQL
    React SPA     (JWT Bearer)   Controllers    Servicios        EF Core DbContext
                                   Swagger      DTOs + reglas    Repository<T> + seed
```

### VetClinic.Domain
Entidades puras (16 tablas), sin dependencias externas.
- `Common/BaseEntity` (Id, CreatedAt, UpdatedAt) y `Enums/Enums.cs` (UserRole, UrgencyLevel, AppointmentStatus, Species, Sex, StockMovementType, HospitalizationStatus, InvoiceStatus).
- Entidades: User, Tutor, Pet, Appointment, MedicalRecord, Prescription, LabResult, Attachment, Hospitalization, HospitalizationEvolution, MedicationAdministration, InventoryItem, StockMovement, Invoice, InvoiceLine, Reminder.

### VetClinic.Application
Contratos y lógica de negocio.
- `DTOs/`: contratos de entrada/salida (Auth, Patient, Appointment, MedicalRecord, Hospitalization, Inventory, Invoice).
- `Interfaces/`: `IRepository<T>` y 8 servicios (`IAuthService`, `IPatientService`, `IAppointmentService`, `IMedicalRecordService`, `IHospitalizationService`, `IInventoryService`, `IInvoiceService`, `IReportService`).
- `Services/`: implementaciones con reglas de negocio (ver sección 6).
- `DependencyInjection.cs`: registro de servicios (`AddApplication`).

### VetClinic.Infrastructure
- `Data/VetClinicDbContext.cs`: configuración EF (longitudes, índices únicos, precisiones decimales, `OnDelete` explícito).
- `Data/SeedData.cs`: datos de demostración (usuarios, tutores, mascotas, inventario, cita, historia clínica).
- `Repositories/Repository.cs`: implementación genérica de `IRepository<T>`.
- `Migrations/`: migraciones `InitialCreate`, `NullableStaffRefs` y `RolesAndPermissions` (rol del usuario como FK a la tabla `Roles` + `PinAccess`).

### VetClinic.Api
- `Program.cs`: DI, JWT (HMAC-SHA256, expiración 12h), CORS, Swagger (solo Development), migraciones auto-aplicadas + seed al arrancar, `JsonStringEnumConverter` (enums como texto en JSON).
- `Controllers/`: 8 controladores REST con `[Authorize(Roles = "...")]`.

---

## 4. Base de datos (esquema relacional)

Documento `docs/architecture.md` guarda el diagrama completo. Resumen de relaciones clave:

- `Tutor 1—N Pet 1—N MedicalRecord` (historia clínica) → N `Prescription` / `LabResult` / `Attachment`.
- `Pet 1—N Appointment` (cita de agenda) → `0..1 MedicalRecord`.
- `Pet 1—N Hospitalization` → N `HospitalizationEvolution` + N `MedicationAdministration`.
- `InventoryItem 1—N StockMovement` (entrada/salida/ajuste que descuenta stock).
- `Tutor 1—N Invoice 1—N InvoiceLine` (servicios e insumos facturados).
- `User 1—N Appointment` (veterinario), `Pet 1—N Reminder`.

---

## 5. Módulos funcionales y endpoints

Método + ruta + rol requerido.

### Autenticación
| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| POST | `/api/auth/login` | público | Login, devuelve JWT + usuario |
| POST | `/api/auth/login-pin` | autenticado | Quick-switch por PIN (cambia de usuario con el PIN propio) |
| GET | `/api/auth/permissions` | autenticado | Permisos del usuario actual desde su rol (`PermissionsJson`) |
| GET | `/api/auth/me` | autenticado | Datos del usuario actual |

### Pacientes y tutores (`patients.view` lectura / `patients.manage` escritura)
| Método | Ruta | Permiso | Descripción |
|---|---|---|---|
| POST | `/api/patients/tutors` | manage | Registrar tutor |
| GET | `/api/patients/tutors` | view | Listar tutores (básico; usado por Facturación) |
| GET | `/api/patients/tutors/list` | view | Listar tutores con cantidad de mascotas |
| GET | `/api/patients/tutors/{id}` | view | Detalle del tutor |
| PUT | `/api/patients/tutors/{id}` | manage | Editar tutor |
| POST | `/api/patients/pets` | manage | Registrar mascota (especie, nacimiento, sexo, color, tutor…) |
| GET | `/api/patients/pets` | view | Listar/buscar mascotas (`search`, `species`, `active`) |
| GET | `/api/patients/pets/{id}` | view | Detalle de mascota (con `ageYears`, `isActive`) |
| PUT | `/api/patients/pets/{id}` | manage | Editar mascota |
| PUT | `/api/patients/pets/{id}/active` | manage | Activar/desactivar (baja lógica) |
| GET | `/api/patients/tutors/{id}/pets` | view | Mascotas de un tutor |
| POST | `/api/patients/pets/{petId}/vaccines` | manage | Registrar vacuna (consume stock FEFO + recordatorio) |
| GET | `/api/patients/pets/{petId}/vaccines` | view | Vacunas de una mascota |
| GET | `/api/patients/vaccines/upcoming?days=30` | view | Próximas dosis (incluye vencidas) |

Cada endpoint exige el permiso indicado mediante la política `perm:<código>` (`RequirePermissionAttribute`); sin él → `403`.

### Agenda (el sistema valida cruces de horarios)
| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/appointments` | Agendar cita (rechaza solapamiento del veterinario) |
| GET | `/api/appointments/upcoming` | Citas próximas |
| GET | `/api/appointments/pet/{petId}` | Historial de citas de una mascota |
| PATCH | `/api/appointments/{id}/status` | Cambiar estado (EnConsulta, Completada, etc.) |

### Historia Clínica (Veterinario / Administrador)
| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/medical-records` | Nueva consulta (síntomas, hallazgos, diagnóstico, tratamiento) |
| GET | `/api/medical-records/pet/{petId}` | Historial completo por mascota **con recetas, lab y adjuntos** |
| GET | `/api/medical-records/{id}` | Detalle de una consulta |
| POST | `/api/medical-records/{id}/prescriptions` | Agregar receta |
| POST | `/api/medical-records/{id}/lab-results` | Agregar análisis de laboratorio |
| POST | `/api/medical-records/{id}/attachments` | Registrar adjunto (rayos X, ecografías) |

### Hospitalización (Veterinario / Administrador)
| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/hospitalizations` | Ingresar paciente (estado inicial `Ingresado`) |
| GET | `/api/hospitalizations/active` | Pacientes hospitalizados activos |
| GET | `/api/hospitalizations/{id}` | Detalle **con evoluciones y medicación** |
| POST | `/api/hospitalizations/{id}/evolutions` | Registrar constantes + observaciones |
| POST | `/api/hospitalizations/{id}/medications` | Registrar administración de medicamento |
| POST | `/api/hospitalizations/{id}/discharge` | Dar de alta |

### Inventario / Farmacia (Veterinario / Administrador)
| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/inventory` | Crear artículo (código único) |
| GET | `/api/inventory` | Listar (incluye flag `lowStock` si stock ≤ mínimo) |
| PATCH | `/api/inventory/{id}/stock` | Entrada (+n) o salida (−n) con movimiento de stock |

### Facturación (todos los roles)
| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/invoices` | Emitir factura (calcula subtotal, IVA configurable `Billing:TaxRate`, total; número FAC-aaaammdd-xxxxxx) |
| GET | `/api/invoices` | Listar facturas |
| GET | `/api/invoices/{id}` | Detalle con líneas |

### Reportes KPI (panel por rol)

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| GET | `/api/reports/dashboard` | Administrador | Ingresos del período (+ variación vs período anterior), facturas, pacientes, clientes, citas programadas, stock bajo |
| GET | `/api/reports/revenues` | Administrador | Ingresos totales y por mes |
| GET | `/api/reports/retention` | Administrador | Retención de clientes (clientes con >1 factura o mascota con >1 cita) |
| GET | `/api/reports/operations` | todos los autenticados | KPIs operativos del día: citas hoy/pendientes/en curso, urgencias, hospitalizados, vacunas próximas, recordatorios y facturas sin cobrar |
| GET | `/api/reports/low-stock` | Administrador, Veterinario | Lista de artículos con stock ≤ mínimo |

Los contratos son **DTOs tipados** (`Application/DTOs/ReportDtos.cs`), no objetos anónimos. `ReportService` está cubierto por `ReportServiceTests` (5 pruebas).

---

## 6. Reglas de negocio implementadas

1. **Hash seguro**: contraseñas con BCrypt (costo default). Nunca se almacenan en claro.
2. **Rol en JWT**: cada token lleva `ClaimTypes.Role` = nombre del rol (tabla `Roles`) y *una claim por permiso*. `[Authorize(Roles=...)]` filtra por nombre de rol; el frontend usa `/api/auth/permissions` para el menú. Recepcionista no puede crear diagnostico/recetas; solo Administrador ve reportes.
3. **Solapamiento de citas**: el servicio de agenda rechaza agendar si el veterinario ya tiene una cita programada cuyo rango (start + duración) se cruza con el nuevo horario → `409 Conflict`.
4. **Citas en el pasado**: se rechazan fechas con más de 5 minutos de retraso.
5. **Stock automático**: la actualización de stock crea un `StockMovement` (Entrada/Salida). No permite stock negativo. La alerta de reposición surge cuando `stockQuantity <= minStock`.
6. **Facturación automática**: subtotal = Σ líneas; IVA = `Billing:TaxRate` (por defecto 16%); total = subtotal + IVA. Número secuencial/único por fecha + GUID corto. Las líneas admiten cantidades fraccionadas (`decimal(12,3)`), salvo en artículos no fraccionables.
7. **Alta de hospitalización**: al dar de alta, estado → `Alta` y se fija `EndDate`.
8. **Integridad referencial**: `OnDelete` explícito por relación (Cascade para hijos, Restrict/SetNull para responsables).
9. **Enums como texto**: `JsonStringEnumConverter` permite enviar `"sex": "Macho"` en JSON (más legible en la API y la UI).

---

## 7. Frontend (React SPA)

- **Protección de rutas**: `RequireAuth` (sin token → `/login`). Guarda token y usuario en `localStorage`.
- **Proxy dev**: Vite reenvía `/api/*` a `http://localhost:8080`, así el navegador no sufre CORS.
- **Login demo** muestra las 3 credenciales seed a un clic.
- **Nav por rol**: cada rol ve solo los módulos que le corresponden (Dashboard→todos; Historia/Hospitalización/Inventario→Vet/Admin; Pacientes/Agenda/Facturación→todos; Configuración→Admin).
- **Configuración con subpestañas** (`pages/settings/`): `UsersPanel` (usuarios y asignación de rol), `RolesPanel` + `PermissionPicker` (catálogo de permisos agrupado por módulo) y `SystemPanel` (parámetros). El backend expone `GET /api/settings/permissions` (catálogo) y CRUD de roles en `RoleService`, validando nombres únicos y permisos contra `Permissions.All`. El rol `Administrador` es de sistema y no se puede renombrar ni editar.

### Pantallas
| Ruta | Pantalla | Funcionalidad |
|---|---|---|
| `/login` | Login | Autenticación JWT |
| `/` | Dashboard | Panel por rol: KPIs operativos (todos) + financieros/gráficos (solo Admin) |
| `/pacientes` | Pacientes | Subpestañas **Pacientes** (buscar/filtrar por nombre/especie/estado, alta/edición, baja lógica y **ficha expandible** con vacunas, consultas y citas), **Tutores** (alta/edición con conteo de mascotas) y **Vacunas y recordatorios** (próximas dosis por rango de días) |
| `/agenda` | Agenda | Crear cita + cambiar estado |
| `/historia` | Historia Clínica | Selección de mascota → consultas con recetas, laboratorio y adjuntos; agregar nueva consulta |
| `/hospitalizacion` | Hospitalización | Ingresar, registrar evolución y medicación, dar de alta |
| `/inventario` | Inventario | Crear artículo, entrada/salida de stock, alertas |
| `/facturacion` | Facturación | Emitir factura (IVA automático) |
| `/configuracion` | Configuración (solo Admin) | Subpestañas **Usuarios** (alta/edición y **asignación de rol**, PIN, contraseña), **Roles** (listar con permisos y usuarios, detalle, crear y editar) y **Parámetros** (IVA, datos del sistema) |

---

## 8. Flujo de trabajo clínico → software (mapeo)

El contexto documentado de la clínica se cubre así:

| Proceso clínico | Dónde se refleja |
|---|---|
| Recepción y triaje → datos del paciente + urgencia | `Pacientes` + `Agenda` (campo `urgency`) |
| Consulta y diagnóstico (síntomas, hallazgos, diagnóstico) | `Historia Clínica` (consulta) |
| Pruebas (rayos X, ecografías, análisis) + resultados en tiempo real | `Historia Clínica` → análisis de laboratorio + adjuntos |
| Tratamiento y hospitalización (constantes, medicación, evolución) | `Hospitalización` |
| Facturación al finalizar (servicios + insumos) | `Facturación` |
| Recordatorios de vacunas/controles | tabla `Reminder` (envío automático pendiente: backlog punto 2) |

---

## 9. Seguridad

- Contraseñas con BCrypt.
- JWT firmado HMAC-SHA256 (clave en `appsettings.json`), expira a las 12h.
- Autorización por rol endpoint por endpoint.
- CORS limitado a política `Frontend` (cualquier origen en demo; restringir a localhost en producción).
- Enums con `JsonStringEnumConverter`; body JSON validado por ModelState (400 ante JSON malformado o tipos incorrectos).

---

## 10. Cómo probar de punta a punta

1. `docker compose up -d postgres`
2. API con `ASPNETCORE_ENVIRONMENT=Development` en `http://localhost:8080` (migra y genera seed solo la primera vez).
3. `npm run dev` en `frontend/` → `http://localhost:5173`.
4. Usuarios: `admin/Admin123!` (Admin), `vet/Vet123!` (Veterinario), `recepcion/Recep123!` (Recepción).
5. Probar: con `vet` crear una consulta en Historia Clínica (seleccionar Rocky), agregar receta, análisis de laboratorio y adjuntar un archivo; en Hospitalización usar "Ingresar paciente", registrar evolución, medicación y dar de alta.

Nota de depuración: al probar con `Invoke-RestMethod` en PowerShell 5.1 los JSON con acentos deben enviarse como bytes UTF-8 (`[Text.Encoding]::UTF8.GetBytes(...)`), porque el cliente codifica ISO-8859-1 por defecto. El navegador no tiene este problema.

---

## 11. Detalles técnicos a recordar

- SDK disponible en la máquina: solo .NET 8 (los paquetes NuGet deben usar versión `8.*`; las versiones `10.*` requieren .NET 10 y fallan).
- `dotnet-ef` instalado global en versión 8.0.31.
- Migraciones se agregan con: `dotnet ef migrations add X --project src/VetClinic.Infrastructure --startup-project src/VetClinic.Api`.
- Para detener la API: `Get-NetTCPConnection -LocalPort 8080 -State Listen` → matar `OwningProcess` (en producción usar systemd/Task Scheduler).
- Docker Compose expone PostgreSQL en `localhost:5432` con usuario `vetapp`/`vetpass123`, DB `vetclinic`, volumen `vetclinic_pgdata`.
- Paquetes de pruebas (`MockQueryable.Moq` y `MockQueryable.EntityFrameworkCore`) fijados a **7.0.3**: sus APIs dependen de `MockQueryable.Core` 7.x; si se sube solo uno a 8.x aparece `MissingMethodException ...MockQueryableExtensions.BuildMock`.

---

## 12. Registro de cambios

### 2026-10-13 — Módulo Pacientes (rework)
1. **Autorización por permisos**: nuevo `RequirePermissionAttribute` y política `perm:<código>` (registrada en `Program.cs` a partir de `Permissions.All`); helper `Permissions.PolicyName` y constante de claim `Permissions.ClaimType`. `PatientsController` exige `patients.view` (lectura) o `patients.manage` (escritura) por endpoint.
2. **Pacientes**: `PatientService` con validaciones (nombre/peso obligatorios, tutor existente), búsqueda y filtro (`ListPetsAsync(search, species, active)`), `SetPetActiveAsync` (baja lógica) y `PetDto` ampliado (`BirthDate`, `Sex`, `Color`, `AgeYears`, `IsActive`). `UpdatePetRequest` ahora incluye `Species`, `BirthDate` y `TutorId`.
3. **Tutores**: `ListTutorsWithPetsAsync`, `GetTutorAsync`, `UpdateTutorAsync` + `TutorListDto` (con dirección y conteo de mascotas).
4. **Vacunas**: `VaccineService.ListUpcomingAsync(days)` + `GET /api/patients/vaccines/upcoming`.
5. **Frontend**: `PatientsPage` con subpestañas (`pages/patients/PatientsPanel`, `TutorsPanel`, `VaccinesPanel`) y ficha expandible (`patients/PatientDetail`); nuevo `api/patients.ts`. Las acciones de escritura se muestran solo con `can('patients.manage')`.
6. **Pruebas**: `PatientServiceTests` ampliado de 3 a 8 casos (validaciones, filtros, baja lógica, no-encontrado). Total **42/42**.

Verificación: `dotnet build` (0 avisos, 0 errores), `dotnet test` (42/42), `npm run lint` (solo warnings preexistentes), `npm run build` (OK) y smoke test contra la API (200 lectura; 403 sin permiso; 401 sin token; 400 validaciones; toggle de baja lógica).

### 2026-10-12 — Correcciones ronda "Bajos"
1. **IVA parametrizable**: nueva sección `Billing:TaxRate` en `appsettings.json` (0.16). `InvoiceService` la lee con `IConfiguration` (`decimal.TryParse`, fallback 16%). Fin del 16% hardcodeado.
2. **Cantidades fraccionadas**: `InvoiceLine.Quantity` pasó de `int` a `decimal` con precisión `numeric(12,3)` (entidad, `VetClinicDbContext`, `InvoiceLineRequest`/`InvoiceLineDto`, `InvoicesPage` con `step="0.001"`). Regla: cantidad > 0 y rechazo de fracciones cuando `IsFractionable = false`. Migración `20261012205638_FractionalInvoiceQuantity`.
3. **Baja de lote (Kardex)**: `InventoryService.DeactivateLotAsync` ahora corre en transacción, registra un `StockMovement` tipo `Ajuste` (cantidad negativa), pone `AvailableQuantity = 0` y clampea `StockQuantity` a ≥ 0 con `Math.Max`. Antes podía dejar stock negativo y no dejaba rastro en Kardex.
4. **Warnings de arranque**:
   - EF Core 20504 (múltiples colecciones en un `Include`) → `QuerySplittingBehavior.SplitQuery` global en `AddInfrastructure`.
   - HttpsRedirection ("Failed to determine the https port") → `UseHttpsRedirection()` solo se registra si hay puerto HTTPS configurado (`ASPNETCORE_HTTPS_PORT` o `HttpsRedirection:HttpsPort`).
5. **Pruebas**: se eliminó el helper casero `AsyncQueryable`/`TestAsyncQueryProvider` y se adoptó `MockQueryable` real (`BuildMockDbSet().Object`), lo que permite afirmar filtrado y orden reales. Se agregaron casos: filtrado por mascota, orden de inventario, transiciones de cita válidas/ inválidas, IVA por defecto/configurado y rechazo de fracción en artículo entero. **11/11 correctas**; salida guardada en `docs/test-results.txt`.

Verificación: `dotnet build` (0 avisos, 0 errores), `dotnet test` (11/11), `npx tsc --noEmit` (OK); migración aplicada y API arrancada en `http://localhost:8080` sin warnings.
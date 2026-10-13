# Arquitectura — VetClinic

## 1. Modelo relacional (PostgreSQL)

```
usuarios (id, username UNIQUE, password_hash, full_name, email, role, is_active, created_at, updated_at)
tutores  (id, first_name, last_name, email, phone, address, created_at, updated_at)
pacientes(id, name, species, breed, birth_date, sex, weight_kg, color, is_active, tutor_id → tutores, ...)

citas────────────────────────────────────────────┐
(id, start_time, duration_minutes, urgency,      │
 status, reason, notes, pet_id → pacientes,      │
 vet_id → usuarios, medical_record_id)           │
                                                 ▼
historias_clinicas (id, record_date, symptoms, findings, diagnosis, treatment_plan,
  pet_id → pacientes, vet_id → usuarios, appointment_id → citas)
  ├── recetas        (id, medication_name, dosage, frequency, duration, instructions,
  │                    medical_record_id)
  ├── resultados_lab (id, test_type, result, reference_range, is_abnormal, result_date,
  │                    medical_record_id)
  └── adjuntos       (id, file_name, file_path, content_type, size_bytes, uploaded_at,
                       medical_record_id)

hospitalizaciones (id, start_date, end_date, status, reason, notes,
  pet_id → pacientes, assigned_vet_id → usuarios)
  ├── evoluciones        (id, recorded_at, temperature, heart_rate, respiratory_rate,
  │                        observations, hospitalization_id, recorded_by_id → usuarios)
  └── administracion_med (id, medication_name, dose, route, administered_at,
                           hospitalization_id, administered_by_id → usuarios)

inventario      (id, name, code UNIQUE, category, unit, stock_quantity, min_stock,
                 unit_price, supplier, is_active)
movimientos_stock (id, type[Entrada|Salida|Ajuste], quantity, unit_cost, description,
                   movement_date, inventory_item_id, performed_by_id → usuarios)

facturas (id, invoice_number UNIQUE, issue_date, status, subtotal, taxes, total,
          payment_method, notes, tutor_id, pet_id, created_by_id)
  └── lineas_factura (id, description, quantity, unit_price, inventory_item_id, invoice_id)

recordatorios (id, recipient, channel[SMS|Correo|WhatsApp], message, scheduled_at,
               sent_at, is_sent, pet_id)
```

## 2. Flujo de trabajo clínico (mapeo a módulos)

1. **Recepción / Triaje** → `citas` (urgency) + `tutores` + `pacientes`. Rol: Recepcionista.
2. **Consulta / Diagnóstico** → `historias_clinicas` + `recetas` + `resultados_lab` + `adjuntos` (rayos X, ecografías). Rol: Veterinario.
3. **Tratamiento / Hospitalización** → `hospitalizaciones` + `evoluciones` + `administracion_med`. Roles: Veterinario/Auxiliar.
4. **Facturación** → `facturas` + `lineas_factura`. Subtotal → IVA 16% → total.
5. **Seguimiento post-consulta** → `recordatorios` (SMS/correo/WhatsApp) programados.

## 3. Regla de stock automático

Al registrar el uso de un producto en la historia clínica, la cita o la hospitalización,
el sistema emite un `movimientos_stock` de tipo **Salida** que descuenta `stock_quantity`.
Cuando `stock_quantity <= min_stock`, el item aparece con `LowStock = true` y se genera
la alerta de reposición (visible en `/api/reports/dashboard` → `lowStockItems`).

## 4. Seguridad

- Contraseñas con **BCrypt** (nunca en texto plano).
- **JWT** firmado (HMAC-SHA256), expiración 12h.
- Autorización por roles vía `[Authorize(Roles = "...")]` en cada controlador.
- DB relacional con claves foráneas y `OnDelete` explícito (Cascade/SetNull/Restrict).

## 5. Arquitectura de despliegue monopuesto (1 equipo)

```
┌───────────────────────────────────────────────┐
│  Equipo local (Windows)                        │
│   • PostgreSQL 16 (contenedor Docker) :5432    │
│   • VetClinic.Api (Kestrel)             :8080   │
│   • Frontend React (Vite dev/estático)  :5173   │
└───────────────────────────────────────────────┘
Backups: copia del volumen Docker o pg_dump; el código vive en git.
```

## 6. Roadmap sugerido

- [x] Fase 1: autenticación, roles, CRUD pacientes/tutores
- [x] Fase 2: historia clínica + agenda
- [x] Fase 3: inventario (descuento automático + alertas)
- [x] Fase 4: facturación (IVA + número secuencial)
- [x] Fase 5: hospitalización + reportes KPI
- [ ] Notificaciones reales (SMS/WhatsApp) para recordatorios
- [ ] Carga de adjuntos (rayos X, ecografías) vía upload multipart
- [ ] Contingencia multi-puesto y backups automáticos
namespace VetClinic.Domain.Enums;

public enum UserRole
{
    Veterinario,
    Recepcionista,
    Administrador
}

public enum UrgencyLevel
{
    Baja,
    Media,
    Alta,
    Emergencia
}

public enum AppointmentStatus
{
    Programada,
    EnEspera,
    EnConsulta,
    Completada,
    Cancelada,
    NoAsistio
}

public enum Species
{
    Perro,
    Gato,
    Ave,
    Reptil,
    Exotico,
    Otro
}

public enum Sex
{
    Macho,
    Hembra
}

public enum StockMovementType
{
    EntradaCompra,
    SalidaVenta,
    SalidaUsoInterno,
    Ajuste
}

public enum HospitalizationStatus
{
    Ingresado,
    EnTratamiento,
    EnRecuperacion,
    Alta,
    Fallecido
}

public enum InvoiceStatus
{
    Borrador,
    Emitida,
    Pagada,
    Anulada
}
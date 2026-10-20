namespace VetClinic.Application.DTOs;

public record PeriodDto(DateTime From, DateTime To);

public record DashboardDto(
    PeriodDto Period,
    decimal Revenue,
    decimal PreviousRevenue,
    decimal? RevenueDeltaPct,
    int InvoicesCount,
    int PetsTotal,
    int ClientsTotal,
    int AppointmentsScheduled,
    int LowStockItems);

public record RevenuePointDto(int Year, int Month, decimal Total, int InvoicesCount);

public record RevenueReportDto(decimal Total, IReadOnlyList<RevenuePointDto> ByMonth);

public record RetentionDto(int TotalClients, int Returning, decimal RetentionRate);

public enum ClinicalGranularity
{
    Dia,
    Semana,
    Mes
}

public record ClinicalTrendPointDto(string Label, DateTime Start, int Count);

/// <summary>
/// Serie temporal de un evento clínico. Los buckets incluyen los periods vacíos
/// para que un hueco en la línea signifique "cero" y no "sin dato".
/// </summary>
public record ClinicalTrendDto(ClinicalGranularity Granularity, int Total, IReadOnlyList<ClinicalTrendPointDto> Points);

public record LowStockItemDto(
    int Id,
    string Name,
    string Category,
    string Code,
    string Unit,
    decimal StockQuantity,
    decimal MinStock,
    string Supplier);

public record OperationsDto(
    int AppointmentsToday,
    int AppointmentsPending,
    int AppointmentsInProgress,
    int UrgentOpen,
    int ActiveHospitalizations,
    int UpcomingVaccines,
    int PendingReminders,
    int UnpaidInvoicesCount,
    decimal UnpaidInvoicesAmount);

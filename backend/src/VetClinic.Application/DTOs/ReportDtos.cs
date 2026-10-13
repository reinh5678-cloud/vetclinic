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

public record RevenuePointDto(int Year, int Month, decimal Total);

public record RevenueReportDto(decimal Total, IReadOnlyList<RevenuePointDto> ByMonth);

public record RetentionDto(int TotalClients, int Returning, decimal RetentionRate);

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

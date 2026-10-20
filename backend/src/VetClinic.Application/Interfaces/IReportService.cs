using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IReportService
{
    Task<DashboardDto> GetDashboardAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<RevenueReportDto> GetRevenuesAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<RetentionDto> GetClientRetentionAsync(CancellationToken ct = default);
    Task<OperationsDto> GetOperationsAsync(CancellationToken ct = default);
    Task<ClinicalTrendDto> GetHospitalizationTrendAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<LowStockItemDto>> GetLowStockAsync(CancellationToken ct = default);
}

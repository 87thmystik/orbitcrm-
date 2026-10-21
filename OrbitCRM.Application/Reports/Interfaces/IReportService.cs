using OrbitCRM.Application.Reports.DTOs;

namespace OrbitCRM.Application.Reports.Interfaces;

public interface IReportService
{
    Task<List<MonthlyValueDto>> GetWonRevenueByMonthAsync(DateRangeDto range, CancellationToken ct = default);
    Task<List<MonthlyValueDto>> GetCustomerGrowthByMonthAsync(DateRangeDto range, CancellationToken ct = default);
    Task<List<SourceBreakdownDto>> GetLeadsBySourceAsync(DateRangeDto range, CancellationToken ct = default);
    Task<List<FunnelStageDto>> GetLeadFunnelAsync(DateRangeDto range, CancellationToken ct = default);
    Task<List<StageBreakdownDto>> GetDealsByPipelineStageAsync(DateRangeDto range, CancellationToken ct = default);
    Task<List<UserPerformanceDto>> GetRevenueBySalespersonAsync(DateRangeDto range, CancellationToken ct = default);
}
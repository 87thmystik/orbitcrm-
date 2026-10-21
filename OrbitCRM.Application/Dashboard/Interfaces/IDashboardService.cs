using OrbitCRM.Application.Dashboard.DTOs;

namespace OrbitCRM.Application.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
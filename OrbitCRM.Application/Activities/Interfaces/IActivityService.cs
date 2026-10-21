using OrbitCRM.Application.Activities.DTOs;
using OrbitCRM.Application.Common.Models;
using OrbitCRM.Domain.Enums;

namespace OrbitCRM.Application.Activities.Interfaces;

public interface IActivityService
{
    Task<PagedResult<ActivityDto>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        ActivityType? type = null,
        Guid? customerId = null,
        Guid? leadId = null,
        Guid? dealId = null,
        CancellationToken cancellationToken = default);

    Task<ActivityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(CreateActivityDto dto, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(UpdateActivityDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<ActivityDto>> GetTimelineForCustomerAsync(Guid customerId, int take = 50, CancellationToken cancellationToken = default);
    Task<List<ActivityDto>> GetTimelineForLeadAsync(Guid leadId, int take = 50, CancellationToken cancellationToken = default);
    Task<List<ActivityDto>> GetTimelineForDealAsync(Guid dealId, int take = 50, CancellationToken cancellationToken = default);
}
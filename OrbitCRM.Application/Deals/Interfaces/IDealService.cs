using OrbitCRM.Application.Common.Models;
using OrbitCRM.Application.Deals.DTOs;

namespace OrbitCRM.Application.Deals.Interfaces;

public interface IDealService
{
    Task<PagedResult<DealDto>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        Guid? pipelineStageId = null,
        CancellationToken cancellationToken = default);

    Task<DealDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(CreateDealDto dto, CancellationToken cancellationToken = default);
    Task<List<DealDto>> GetDealsForCustomerAsync(Guid customerId, CancellationToken ct = default);

    Task<Result> UpdateAsync(UpdateDealDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> MarkAsWonAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> MarkAsLostAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<PipelineStageDto>> GetPipelineStagesAsync(CancellationToken cancellationToken = default);
}
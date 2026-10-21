using OrbitCRM.Application.Common.Models;
using OrbitCRM.Application.Leads.DTOs;
using OrbitCRM.Domain.Enums;

namespace OrbitCRM.Application.Leads.Interfaces;

public interface ILeadService
{
    Task<PagedResult<LeadDto>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        LeadStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<LeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(CreateLeadDto dto, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(UpdateLeadDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> ConvertToCustomerAsync(Guid leadId, CancellationToken cancellationToken = default);
}
using OrbitCRM.Application.Common.Models;
using OrbitCRM.Application.Customers.DTOs;

namespace OrbitCRM.Application.Customers.Interfaces;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(UpdateCustomerDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
using OrbitCRM.Application.Common.Models;
using OrbitCRM.Application.Tasks.DTOs;

namespace OrbitCRM.Application.Tasks.Interfaces;

public interface ITaskService
{
    Task<PagedResult<TaskDto>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? filter = null,
        CancellationToken cancellationToken = default);

    Task<TaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default); Task<List<TaskDto>> GetTasksForCustomerAsync(Guid customerId, CancellationToken ct = default);

    Task<Result<Guid>> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(UpdateTaskDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> CompleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> ReopenAsync(Guid id, CancellationToken cancellationToken = default);
}
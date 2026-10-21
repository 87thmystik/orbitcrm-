using Microsoft.EntityFrameworkCore;
using OrbitCRM.Application.Common.Models;
using OrbitCRM.Application.Tasks.DTOs;
using OrbitCRM.Application.Tasks.Interfaces;
using OrbitCRM.Domain.Entities;
using OrbitCRM.Domain.Enums;
using OrbitCRM.Infrastructure.Data;
using OrbitCRM.Infrastructure.Identity;

namespace OrbitCRM.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly OrbitCRMDbContext _db;
    private readonly TenantGuard _tenantGuard;

    public TaskService(OrbitCRMDbContext db, TenantGuard tenantGuard)
    {
        _db = db;
        _tenantGuard = tenantGuard;
    }
    public async Task<List<TaskDto>> GetTasksForCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();
        return await _db.Tasks
            .AsNoTracking()
            .Where(t => t.CustomerId == customerId && t.Status != TaskItemStatus.Completed && t.Status != TaskItemStatus.Cancelled)
            .OrderBy(t => t.DueDate)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                DueDate = t.DueDate,
                Priority = t.Priority,
                Status = t.Status
            })
            .ToListAsync(ct);
    }

    public async Task<PagedResult<TaskDto>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync(); // ← CRITICAL FIX

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Tasks
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.Lead)
            .Include(t => t.Deal)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t =>
                EF.Functions.ILike(t.Title, $"%{term}%") ||
                (t.Description != null && EF.Functions.ILike(t.Description, $"%{term}%")));
        }

        var today = DateTime.UtcNow.Date;

        query = filter switch
        {
            TaskFilter.Open => query.Where(t =>
                t.Status != TaskItemStatus.Completed &&
                t.Status != TaskItemStatus.Cancelled),

            TaskFilter.Overdue => query.Where(t =>
                t.DueDate.HasValue &&
                t.DueDate.Value < today &&
                t.Status != TaskItemStatus.Completed &&
                t.Status != TaskItemStatus.Cancelled),

            TaskFilter.Completed => query.Where(t => t.Status == TaskItemStatus.Completed),

            _ => query
        };

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.Status == TaskItemStatus.Completed ? 1 : 0)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.Priority)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                DueDate = t.DueDate,
                Priority = t.Priority,
                Status = t.Status,
                CompletedAt = t.CompletedAt,
                CustomerId = t.CustomerId,
                CustomerName = t.Customer != null ? t.Customer.FirstName + " " + t.Customer.LastName : null,
                LeadId = t.LeadId,
                LeadName = t.Lead != null ? t.Lead.FirstName + " " + t.Lead.LastName : null,
                DealId = t.DealId,
                DealName = t.Deal != null ? t.Deal.Name : null,
                AssignedToUserId = t.AssignedToUserId,
                CreatedByUserId = t.CreatedByUserId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync(); // ← CRITICAL FIX

        return await _db.Tasks
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.Lead)
            .Include(t => t.Deal)
            .Where(t => t.Id == id)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                DueDate = t.DueDate,
                Priority = t.Priority,
                Status = t.Status,
                CompletedAt = t.CompletedAt,
                CustomerId = t.CustomerId,
                CustomerName = t.Customer != null ? t.Customer.FirstName + " " + t.Customer.LastName : null,
                LeadId = t.LeadId,
                LeadName = t.Lead != null ? t.Lead.FirstName + " " + t.Lead.LastName : null,
                DealId = t.DealId,
                DealName = t.Deal != null ? t.Deal.Name : null,
                AssignedToUserId = t.AssignedToUserId,
                CreatedByUserId = t.CreatedByUserId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<Guid>> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync(); // ← CRITICAL FIX

        var task = new TaskItem
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            DueDate = dto.DueDate,
            Priority = dto.Priority,
            Status = TaskItemStatus.Pending,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            DealId = dto.DealId
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(task.Id);
    }

    public async Task<Result> UpdateAsync(UpdateTaskDto dto, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync(); // ← CRITICAL FIX

        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (task is null) return Result.Failure("Task not found.");

        task.Title = dto.Title.Trim();
        task.Description = dto.Description?.Trim();
        task.DueDate = dto.DueDate;
        task.Priority = dto.Priority;
        task.Status = dto.Status;
        task.CustomerId = dto.CustomerId;
        task.LeadId = dto.LeadId;
        task.DealId = dto.DealId;

        if (dto.Status == TaskItemStatus.Completed && task.CompletedAt is null)
            task.CompletedAt = DateTime.UtcNow;
        else if (dto.Status != TaskItemStatus.Completed)
            task.CompletedAt = null;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync(); // ← CRITICAL FIX

        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task is null) return Result.Failure("Task not found.");

        task.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync(); // ← CRITICAL FIX

        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task is null) return Result.Failure("Task not found.");

        task.Status = TaskItemStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ReopenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync(); // ← CRITICAL FIX

        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task is null) return Result.Failure("Task not found.");

        task.Status = TaskItemStatus.Pending;
        task.CompletedAt = null;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
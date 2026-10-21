using Microsoft.EntityFrameworkCore;
using OrbitCRM.Application.Activities.DTOs;
using OrbitCRM.Application.Activities.Interfaces;
using OrbitCRM.Application.Common.Models;
using OrbitCRM.Domain.Entities;
using OrbitCRM.Domain.Enums;
using OrbitCRM.Infrastructure.Data;
using OrbitCRM.Infrastructure.Identity;

namespace OrbitCRM.Infrastructure.Services;

public class ActivityService : IActivityService
{
    private readonly OrbitCRMDbContext _db;
    private readonly TenantGuard _tenantGuard;

    public ActivityService(OrbitCRMDbContext db, TenantGuard tenantGuard)
    {
        _db = db;
        _tenantGuard = tenantGuard;
    }

    public async Task<PagedResult<ActivityDto>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        ActivityType? type = null,
        Guid? customerId = null,
        Guid? leadId = null,
        Guid? dealId = null,
        CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.Deal)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a =>
                EF.Functions.ILike(a.Subject, $"%{term}%") ||
                (a.Description != null && EF.Functions.ILike(a.Description, $"%{term}%")));
        }

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        if (customerId.HasValue)
            query = query.Where(a => a.CustomerId == customerId.Value);

        if (leadId.HasValue)
            query = query.Where(a => a.LeadId == leadId.Value);

        if (dealId.HasValue)
            query = query.Where(a => a.DealId == dealId.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToDto(a))
            .ToListAsync(cancellationToken);

        return new PagedResult<ActivityDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ActivityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        return await _db.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.Deal)
            .Where(a => a.Id == id)
            .Select(a => MapToDto(a))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<Guid>> CreateAsync(CreateActivityDto dto, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var activity = new CrmActivity
        {
            Type = dto.Type,
            Subject = dto.Subject.Trim(),
            Description = dto.Description?.Trim(),
            OccurredAt = dto.OccurredAt,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            DealId = dto.DealId
        };

        _db.Activities.Add(activity);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(activity.Id);
    }

    public async Task<Result> UpdateAsync(UpdateActivityDto dto, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var activity = await _db.Activities
            .FirstOrDefaultAsync(a => a.Id == dto.Id, cancellationToken);

        if (activity is null) return Result.Failure("Activity not found.");

        activity.Type = dto.Type;
        activity.Subject = dto.Subject.Trim();
        activity.Description = dto.Description?.Trim();
        activity.OccurredAt = dto.OccurredAt;
        activity.CustomerId = dto.CustomerId;
        activity.LeadId = dto.LeadId;
        activity.DealId = dto.DealId;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var activity = await _db.Activities
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (activity is null) return Result.Failure("Activity not found.");

        activity.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<List<ActivityDto>> GetTimelineForCustomerAsync(Guid customerId, int take = 50, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        return await _db.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.Deal)
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.OccurredAt)
            .Take(take)
            .Select(a => MapToDto(a))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ActivityDto>> GetTimelineForLeadAsync(Guid leadId, int take = 50, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        return await _db.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.Deal)
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.OccurredAt)
            .Take(take)
            .Select(a => MapToDto(a))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ActivityDto>> GetTimelineForDealAsync(Guid dealId, int take = 50, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        return await _db.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.Deal)
            .Where(a => a.DealId == dealId)
            .OrderByDescending(a => a.OccurredAt)
            .Take(take)
            .Select(a => MapToDto(a))
            .ToListAsync(cancellationToken);
    }

    private static ActivityDto MapToDto(CrmActivity a) => new()
    {
        Id = a.Id,
        Type = a.Type,
        Subject = a.Subject,
        Description = a.Description,
        OccurredAt = a.OccurredAt,
        UserId = a.UserId,
        CustomerId = a.CustomerId,
        CustomerName = a.Customer != null ? a.Customer.FirstName + " " + a.Customer.LastName : null,
        LeadId = a.LeadId,
        LeadName = a.Lead != null ? a.Lead.FirstName + " " + a.Lead.LastName : null,
        DealId = a.DealId,
        DealName = a.Deal != null ? a.Deal.Name : null,
        CreatedAt = a.CreatedAt
    };
}
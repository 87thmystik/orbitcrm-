using Microsoft.EntityFrameworkCore;
using OrbitCRM.Application.Audit.DTOs;
using OrbitCRM.Application.Audit.Interfaces;
using OrbitCRM.Application.Common.Models;
using OrbitCRM.Infrastructure.Data;
using OrbitCRM.Infrastructure.Identity;

namespace OrbitCRM.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly OrbitCRMDbContext _db;
    private readonly TenantGuard _tenantGuard;

    public AuditService(OrbitCRMDbContext db, TenantGuard tenantGuard)
    {
        _db = db;
        _tenantGuard = tenantGuard;
    }

    public async Task<PagedResult<AuditLogDto>> GetAllAsync(
        int page = 1,
        int pageSize = 50,
        string? entityName = null,
        string? action = null,
        string? userEmail = null,
        CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrWhiteSpace(userEmail))
            query = query.Where(a => a.UserEmail != null && a.UserEmail.Contains(userEmail));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserEmail = a.UserEmail,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Action = a.Action,
                Changes = a.Changes,
                Timestamp = a.Timestamp
            })
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<string>> GetEntityNamesAsync(CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        return await _db.AuditLogs
            .AsNoTracking()
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(ct);
    }
}
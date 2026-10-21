using OrbitCRM.Application.Audit.DTOs;
using OrbitCRM.Application.Common.Models;

namespace OrbitCRM.Application.Audit.Interfaces;

public interface IAuditService
{
    Task<PagedResult<AuditLogDto>> GetAllAsync(
        int page = 1,
        int pageSize = 50,
        string? entityName = null,
        string? action = null,
        string? userEmail = null,
        CancellationToken ct = default);

    Task<List<string>> GetEntityNamesAsync(CancellationToken ct = default);
}
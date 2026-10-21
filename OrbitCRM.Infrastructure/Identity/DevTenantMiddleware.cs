using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrbitCRM.Infrastructure.Data;

namespace OrbitCRM.Infrastructure.Identity;

/// <summary>
/// DEV-ONLY middleware. Sets CurrentTenant to the first tenant in the database
/// so query filters return data during development. Replaced by claim-based
/// resolution once login is wired up.
/// </summary>
public class DevTenantMiddleware
{
    private readonly RequestDelegate _next;

    public DevTenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        CurrentTenant currentTenant,
        OrbitCRMDbContext db)
    {
        if (!currentTenant.HasTenant)
        {
            var devTenant = await db.Tenants
                .AsNoTracking()
                .IgnoreQueryFilters() // Tenants table has no tenant filter, but for clarity
                .OrderBy(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (devTenant is not null)
            {
                currentTenant.SetTenant(devTenant.Id, devTenant.Slug);
            }
        }

        await _next(context);
    }
}
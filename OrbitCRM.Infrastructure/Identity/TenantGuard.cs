using Microsoft.EntityFrameworkCore;
using OrbitCRM.Infrastructure.Data;

namespace OrbitCRM.Infrastructure.Identity;

public class TenantGuard
{
    private readonly CurrentTenant _currentTenant;
    private readonly OrbitCRMDbContext _db;

    public TenantGuard(CurrentTenant currentTenant, OrbitCRMDbContext db)
    {
        _currentTenant = currentTenant;
        _db = db;
    }

    public async Task EnsureAsync()
    {
        // Tenant is already set from auth claims — nothing to do.
        if (_currentTenant.HasTenant) return;

        // Fallback for dev/unseeded scenarios: pick the first tenant.
        // In production this should never be reached once auth is wired up.
        var tenant = await _db.Tenants
            .AsNoTracking()
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (tenant is not null)
        {
            _currentTenant.SetTenant(tenant.Id, tenant.Slug);
        }
    }
}
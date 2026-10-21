using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrbitCRM.Application.Common.Interfaces;

namespace OrbitCRM.Infrastructure.Identity;

public class CurrentTenant : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _tenantId;
    private string? _tenantSlug;

    public CurrentTenant(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            if (_tenantId.HasValue && _tenantId.Value != Guid.Empty)
                return _tenantId.Value;

            var claimValue = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(TenantClaims.TenantId);

            if (Guid.TryParse(claimValue, out var id))
                return id;

            return Guid.Empty;
        }
    }

    public string? TenantSlug
    {
        get
        {
            if (!string.IsNullOrEmpty(_tenantSlug)) return _tenantSlug;

            return _httpContextAccessor.HttpContext?.User
                .FindFirstValue(TenantClaims.TenantSlug);
        }
    }

    public bool HasTenant => TenantId != Guid.Empty;

    public void SetTenant(Guid tenantId, string? slug = null)
    {
        _tenantId = tenantId;
        _tenantSlug = slug;
    }
}
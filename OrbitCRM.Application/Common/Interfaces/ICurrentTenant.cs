namespace OrbitCRM.Application.Common.Interfaces;

public interface ICurrentTenant
{
    Guid TenantId { get; }
    string? TenantSlug { get; }
    bool HasTenant { get; }
    void SetTenant(Guid tenantId, string? slug = null);
}
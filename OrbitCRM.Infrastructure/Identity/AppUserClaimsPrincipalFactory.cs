using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrbitCRM.Domain.Identity;
using OrbitCRM.Infrastructure.Data;

namespace OrbitCRM.Infrastructure.Identity;

public class AppUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
{
    private readonly OrbitCRMDbContext _db;
    private readonly ILogger<AppUserClaimsPrincipalFactory> _logger;

    public AppUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityOptions> options,
        OrbitCRMDbContext db,
        ILogger<AppUserClaimsPrincipalFactory> logger)
        : base(userManager, roleManager, options)
    {
        _db = db;
        _logger = logger;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Profile claims
        if (!string.IsNullOrEmpty(user.FirstName))
            identity.AddClaim(new Claim("given_name", user.FirstName));
        if (!string.IsNullOrEmpty(user.LastName))
            identity.AddClaim(new Claim("family_name", user.LastName));

        // Active membership for this user
        var membership = await _db.TenantMemberships
            .Include(m => m.Tenant)
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.IsActive);

        if (membership is null)
        {
            _logger.LogWarning("Claims factory: no active membership for {Email}", user.Email);
            return identity;
        }

        // Tenant claims
        identity.AddClaim(new Claim(TenantClaims.TenantId, membership.TenantId.ToString()));
        if (membership.Tenant is not null)
        {
            identity.AddClaim(new Claim(TenantClaims.TenantSlug, membership.Tenant.Slug));
        }

        // Role claim — from membership, not Identity roles
        var role = string.IsNullOrEmpty(membership.Role) ? Roles.SalesRep : membership.Role;
        identity.AddClaim(new Claim(ClaimTypes.Role, role));

        // Permission claims — expand role into individual permissions
        foreach (var permission in RolePermissions.GetPermissionsForRole(role))
        {
            identity.AddClaim(new Claim("permission", permission));
        }

        _logger.LogWarning(
            "Claims factory: user={Email}, role={Role}, permissions={Count}",
            user.Email, role, identity.Claims.Count(c => c.Type == "permission"));

        return identity;
    }
}
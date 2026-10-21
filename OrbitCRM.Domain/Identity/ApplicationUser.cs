using Microsoft.AspNetCore.Identity;
using OrbitCRM.Domain.Entities;

namespace OrbitCRM.Domain.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public ICollection<TenantMembership> TenantMemberships { get; set; } = new List<TenantMembership>();
}
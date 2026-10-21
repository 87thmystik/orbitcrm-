using OrbitCRM.Domain.Common;

namespace OrbitCRM.Domain.Entities;

public class Customer : TenantEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
}
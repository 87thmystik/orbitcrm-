using OrbitCRM.Domain.Common;
using OrbitCRM.Domain.Enums;

namespace OrbitCRM.Domain.Entities;

public class Lead : TenantEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? Source { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public Priority Priority { get; set; } = Priority.Medium;
    public decimal EstimatedValue { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
}
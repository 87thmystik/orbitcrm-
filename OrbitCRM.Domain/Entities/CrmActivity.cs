using OrbitCRM.Domain.Common;
using OrbitCRM.Domain.Enums;

namespace OrbitCRM.Domain.Entities;

public class CrmActivity : TenantEntity
{
    public ActivityType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Guid? UserId { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public Guid? DealId { get; set; }
    public Deal? Deal { get; set; }
}
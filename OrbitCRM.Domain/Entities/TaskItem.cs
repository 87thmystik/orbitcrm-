using OrbitCRM.Domain.Common;
using OrbitCRM.Domain.Enums;

namespace OrbitCRM.Domain.Entities;

public class TaskItem : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
    public DateTime? CompletedAt { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public Guid? DealId { get; set; }
    public Deal? Deal { get; set; }
}
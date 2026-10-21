using OrbitCRM.Domain.Enums;

namespace OrbitCRM.Application.Activities.DTOs;

public class ActivityDto
{
    public Guid Id { get; set; }
    public ActivityType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; }

    public Guid? UserId { get; set; }

    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }

    public Guid? LeadId { get; set; }
    public string? LeadName { get; set; }

    public Guid? DealId { get; set; }
    public string? DealName { get; set; }

    public DateTime CreatedAt { get; set; }

    public string RelatedSummary
    {
        get
        {
            if (!string.IsNullOrEmpty(CustomerName)) return CustomerName!;
            if (!string.IsNullOrEmpty(LeadName)) return LeadName!;
            if (!string.IsNullOrEmpty(DealName)) return DealName!;
            return "—";
        }
    }
}

public class CreateActivityDto
{
    public ActivityType Type { get; set; } = ActivityType.Note;
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Guid? CustomerId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? DealId { get; set; }
}

public class UpdateActivityDto
{
    public Guid Id { get; set; }
    public ActivityType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; }

    public Guid? CustomerId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? DealId { get; set; }
}
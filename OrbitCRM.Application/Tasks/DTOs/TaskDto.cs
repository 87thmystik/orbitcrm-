using OrbitCRM.Domain.Enums;

namespace OrbitCRM.Application.Tasks.DTOs;

public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public Priority Priority { get; set; }
    public TaskItemStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }

    public Guid? LeadId { get; set; }
    public string? LeadName { get; set; }

    public Guid? DealId { get; set; }
    public string? DealName { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsOverdue =>
        DueDate.HasValue &&
        Status != TaskItemStatus.Completed &&
        Status != TaskItemStatus.Cancelled &&
        DueDate.Value.Date < DateTime.UtcNow.Date;

    public bool IsDueToday =>
        DueDate.HasValue &&
        Status != TaskItemStatus.Completed &&
        Status != TaskItemStatus.Cancelled &&
        DueDate.Value.Date == DateTime.UtcNow.Date;

    public string RelatedName =>
        CustomerName ?? LeadName ?? DealName ?? string.Empty;
}

public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public Guid? CustomerId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? DealId { get; set; }
}

public class UpdateTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public Priority Priority { get; set; }
    public TaskItemStatus Status { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? DealId { get; set; }
}

public static class TaskFilter
{
    public const string All = "all";
    public const string Open = "open";
    public const string Overdue = "overdue";
    public const string Completed = "completed";
}
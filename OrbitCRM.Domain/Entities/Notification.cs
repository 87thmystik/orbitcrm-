namespace OrbitCRM.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    /// <summary>
    /// The user who should see this notification.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Short headline — e.g. "Deal won: Acme Renewal"
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional supporting text.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Optional URL to navigate to when clicked.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Optional icon hint — "deal", "task", "lead", "customer"
    /// </summary>
    public string? Kind { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
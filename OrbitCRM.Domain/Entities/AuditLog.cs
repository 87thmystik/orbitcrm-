namespace OrbitCRM.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }

    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    /// <summary>
    /// Created | Updated | Deleted
    /// </summary>
    public string Action { get; set; } = string.Empty;

    public string? Changes { get; set; } // JSON

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
namespace OrbitCRM.Application.Notifications.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Url { get; set; }
    public string? Kind { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
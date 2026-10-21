using OrbitCRM.Application.Notifications.DTOs;

namespace OrbitCRM.Application.Notifications.Interfaces;

public interface INotificationService
{
    Task<int> GetUnreadCountAsync(CancellationToken ct = default);
    Task<List<NotificationDto>> GetRecentAsync(int take = 10, CancellationToken ct = default);
    Task<List<NotificationDto>> GetAllAsync(int page, int pageSize, bool? isRead, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid id, CancellationToken ct = default);
    Task MarkAllAsReadAsync(CancellationToken ct = default);
}
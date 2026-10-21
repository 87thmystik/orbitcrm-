using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrbitCRM.Application.Common.Interfaces;
using OrbitCRM.Application.Notifications.DTOs;
using OrbitCRM.Application.Notifications.Interfaces;
using OrbitCRM.Infrastructure.Data;
using OrbitCRM.Infrastructure.Identity;

namespace OrbitCRM.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUser _currentUser;

    public NotificationService(IServiceScopeFactory scopeFactory, ICurrentUser currentUser)
    {
        _scopeFactory = scopeFactory;
        _currentUser = currentUser;
    }

    // Helper to create an isolated scope for background/parallel tasks
    private async Task<T> ExecuteWithScopeAsync<T>(Func<OrbitCRMDbContext, Task<T>> action)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrbitCRMDbContext>();
        var tenantGuard = scope.ServiceProvider.GetRequiredService<TenantGuard>();

        await tenantGuard.EnsureAsync();
        return await action(db);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken ct = default)
    {
        if (!_currentUser.UserId.HasValue) return 0;

        return await ExecuteWithScopeAsync(async db =>
            await db.Notifications.CountAsync(n => n.UserId == _currentUser.UserId.Value && !n.IsRead, ct));
    }

    public async Task<List<NotificationDto>> GetRecentAsync(int take = 10, CancellationToken ct = default)
    {
        if (!_currentUser.UserId.HasValue) return new();

        return await ExecuteWithScopeAsync(async db =>
            await db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == _currentUser.UserId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Url = n.Url,
                    Kind = n.Kind,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync(ct));
    }

    public async Task<List<NotificationDto>> GetAllAsync(int page, int pageSize, bool? isRead, CancellationToken ct = default)
    {
        if (!_currentUser.UserId.HasValue) return new();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        return await ExecuteWithScopeAsync(async db =>
        {
            var query = db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == _currentUser.UserId.Value);

            if (isRead.HasValue)
                query = query.Where(n => n.IsRead == isRead.Value);

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Url = n.Url,
                    Kind = n.Kind,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync(ct);
        });
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken ct = default)
    {
        if (!_currentUser.UserId.HasValue) return;

        await ExecuteWithScopeAsync(async db =>
        {
            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == _currentUser.UserId.Value, ct);

            if (notification is not null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            return true;
        });
    }

    public async Task MarkAllAsReadAsync(CancellationToken ct = default)
    {
        if (!_currentUser.UserId.HasValue) return;

        await ExecuteWithScopeAsync(async db =>
        {
            var unread = await db.Notifications
                .Where(n => n.UserId == _currentUser.UserId.Value && !n.IsRead)
                .ToListAsync(ct);

            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            return true;
        });
    }
}
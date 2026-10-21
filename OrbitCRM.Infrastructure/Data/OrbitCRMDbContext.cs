using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OrbitCRM.Application.Common.Interfaces;
using OrbitCRM.Domain.Common;
using OrbitCRM.Domain.Entities;
using OrbitCRM.Domain.Enums;
using OrbitCRM.Domain.Identity;

namespace OrbitCRM.Infrastructure.Data;

public class OrbitCRMDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public OrbitCRMDbContext(
        DbContextOptions<OrbitCRMDbContext> options,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser) : base(options)
    {
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    // Tenant
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // CRM
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<CrmActivity> Activities => Set<CrmActivity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(OrbitCRMDbContext).Assembly);

        // Global query filters — tenant isolation + soft delete
        builder.Entity<Customer>().HasQueryFilter(e =>
            e.TenantId == _currentTenant.TenantId && !e.IsDeleted);
        builder.Entity<Lead>().HasQueryFilter(e =>
            e.TenantId == _currentTenant.TenantId && !e.IsDeleted);
        builder.Entity<PipelineStage>().HasQueryFilter(e =>
            e.TenantId == _currentTenant.TenantId && !e.IsDeleted);
        builder.Entity<Deal>().HasQueryFilter(e =>
            e.TenantId == _currentTenant.TenantId && !e.IsDeleted);
        builder.Entity<TaskItem>().HasQueryFilter(e =>
            e.TenantId == _currentTenant.TenantId && !e.IsDeleted);
        builder.Entity<CrmActivity>().HasQueryFilter(e =>
            e.TenantId == _currentTenant.TenantId && !e.IsDeleted);

        // FIX: Do NOT put a UserId filter on Notifications here. 
        // We filter by user in NotificationService to avoid startup crashes.
        builder.Entity<Notification>().HasQueryFilter(n => n.TenantId == _currentTenant.TenantId);

        // AuditLog has a query filter but no IsDeleted
        builder.Entity<AuditLog>().HasQueryFilter(e =>
            e.TenantId == _currentTenant.TenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var (userEmail, userId) = GetAuditContext();
        ApplyAuditFields();

        var auditLogs = BuildAuditLogs(userEmail, userId);
        if (auditLogs.Count > 0) AuditLogs.AddRange(auditLogs);

        var autoActivities = BuildAutoActivities();
        if (autoActivities.Count > 0) Activities.AddRange(autoActivities);

        var notifications = BuildNotifications();
        if (notifications.Count > 0) Notifications.AddRange(notifications);

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        var (userEmail, userId) = GetAuditContext();
        ApplyAuditFields();

        var auditLogs = BuildAuditLogs(userEmail, userId);
        if (auditLogs.Count > 0) AuditLogs.AddRange(auditLogs);

        var autoActivities = BuildAutoActivities();
        if (autoActivities.Count > 0) Activities.AddRange(autoActivities);

        var notifications = BuildNotifications();
        if (notifications.Count > 0) Notifications.AddRange(notifications);

        return base.SaveChanges();
    }

    private void ApplyAuditFields()
    {
        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (!_currentTenant.HasTenant)
                    {
                        var devTenantId = ResolveDevTenantId();
                        if (devTenantId == Guid.Empty)
                        {
                            throw new InvalidOperationException(
                                $"Cannot save {entry.Entity.GetType().Name} without a current tenant.");
                        }
                    }

                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = null;
                    entry.Entity.TenantId = _currentTenant.TenantId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.TenantId = entry.OriginalValues.GetValue<Guid>(
                        nameof(TenantEntity.TenantId));
                    break;
            }

            NormalizeDateTimes(entry);
        }
    }

    private static void NormalizeDateTimes(EntityEntry entry)
    {
        foreach (var property in entry.Properties)
        {
            if (property.CurrentValue is DateTime dt && dt.Kind != DateTimeKind.Utc)
            {
                property.CurrentValue = dt.Kind == DateTimeKind.Local
                    ? dt.ToUniversalTime()
                    : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
        }
    }

    private Guid ResolveDevTenantId()
    {
        try
        {
            var firstTenant = Tenants
                .AsNoTracking()
                .OrderBy(t => t.CreatedAt)
                .Select(t => new { t.Id, t.Slug })
                .FirstOrDefault();

            if (firstTenant is not null)
            {
                _currentTenant.SetTenant(firstTenant.Id, firstTenant.Slug);
                return firstTenant.Id;
            }
        }
        catch
        {
            // Swallow exceptions
        }

        return Guid.Empty;
    }

    private List<AuditLog> BuildAuditLogs(string? userEmail, Guid? userId)
    {
        var logs = new List<AuditLog>();
        var now = DateTime.UtcNow;
        var skipTypes = new HashSet<string> { nameof(AuditLog) };

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not TenantEntity tenantEntity) continue;
            if (skipTypes.Contains(entry.Entity.GetType().Name)) continue;

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Modified => AuditAction.Updated,
                EntityState.Deleted => AuditAction.Deleted,
                _ => null
            };

            if (action is null) continue;

            if (entry.State == EntityState.Modified &&
                entry.Property(nameof(TenantEntity.IsDeleted)).IsModified &&
                (bool)entry.Property(nameof(TenantEntity.IsDeleted)).CurrentValue! == true)
            {
                action = AuditAction.Deleted;
            }

            var changes = new Dictionary<string, object?>();

            if (entry.State == EntityState.Added)
            {
                foreach (var prop in entry.Properties)
                {
                    var name = prop.Metadata.Name;
                    if (name is "TenantId" or "CreatedAt" or "UpdatedAt") continue;
                    changes[name] = prop.CurrentValue;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                foreach (var prop in entry.Properties)
                {
                    if (!prop.IsModified) continue;
                    var name = prop.Metadata.Name;
                    if (name is "UpdatedAt") continue;

                    changes[name] = new
                    {
                        Old = prop.OriginalValue,
                        New = prop.CurrentValue
                    };
                }
                if (changes.Count == 0) continue;
            }
            else if (entry.State == EntityState.Deleted)
            {
                foreach (var prop in entry.Properties)
                {
                    changes[prop.Metadata.Name] = prop.OriginalValue;
                }
            }

            logs.Add(new AuditLog
            {
                TenantId = tenantEntity.TenantId,
                UserId = userId,
                UserEmail = userEmail,
                EntityName = entry.Entity.GetType().Name,
                EntityId = tenantEntity.Id,
                Action = action,
                Changes = System.Text.Json.JsonSerializer.Serialize(changes),
                Timestamp = now
            });
        }

        return logs;
    }

    private List<CrmActivity> BuildAutoActivities()
    {
        var activities = new List<CrmActivity>();
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is CrmActivity) continue;

            string? subject = null;
            Guid? customerId = null;
            Guid? leadId = null;
            Guid? dealId = null;
            ActivityType type = ActivityType.Note;

            switch (entry.Entity)
            {
                case Customer customer when entry.State == EntityState.Added:
                    subject = $"Customer created: {customer.FirstName} {customer.LastName}";
                    customerId = customer.Id;
                    break;

                case Customer customer when entry.State == EntityState.Modified:
                    var customerDeletedProp = entry.Property(nameof(TenantEntity.IsDeleted));
                    if (customerDeletedProp.IsModified && (bool)customerDeletedProp.CurrentValue!)
                    {
                        subject = $"Customer deleted: {customer.FirstName} {customer.LastName}";
                        customerId = customer.Id;
                    }
                    break;

                case Lead lead when entry.State == EntityState.Added:
                    subject = $"Lead created: {lead.FirstName} {lead.LastName}";
                    leadId = lead.Id;
                    break;

                case Lead lead when entry.State == EntityState.Modified:
                    var leadStatusProp = entry.Property(nameof(Lead.Status));
                    if (leadStatusProp.IsModified)
                    {
                        var oldStatus = leadStatusProp.OriginalValue;
                        var newStatus = leadStatusProp.CurrentValue;

                        if (newStatus?.ToString() == "Converted")
                            subject = $"Lead converted to customer: {lead.FirstName} {lead.LastName}";
                        else
                            subject = $"Lead status changed: {lead.FirstName} {lead.LastName} ({oldStatus} → {newStatus})";

                        leadId = lead.Id;
                    }
                    break;

                case Deal deal when entry.State == EntityState.Added:
                    subject = $"Deal created: {deal.Name}";
                    dealId = deal.Id;
                    break;

                case Deal deal when entry.State == EntityState.Modified:
                    var dealStatusProp = entry.Property(nameof(Deal.Status));
                    if (dealStatusProp.IsModified)
                    {
                        var newStatus = dealStatusProp.CurrentValue;

                        if (newStatus?.ToString() == "Won")
                            subject = $"Deal won: {deal.Name} — {deal.Value:C0}";
                        else if (newStatus?.ToString() == "Lost")
                            subject = $"Deal lost: {deal.Name}";
                        else if (newStatus?.ToString() == "Abandoned")
                            subject = $"Deal abandoned: {deal.Name}";
                        else
                            subject = $"Deal status changed: {deal.Name}";

                        dealId = deal.Id;
                    }
                    break;

                case TaskItem task when entry.State == EntityState.Added:
                    subject = $"Task created: {task.Title}";
                    break;

                case TaskItem task when entry.State == EntityState.Modified:
                    var taskStatusProp = entry.Property(nameof(TaskItem.Status));
                    if (taskStatusProp.IsModified && taskStatusProp.CurrentValue?.ToString() == "Completed")
                    {
                        subject = $"Task completed: {task.Title}";
                    }
                    break;
            }

            if (subject is not null)
            {
                var triggeredBy = entry.Entity as TenantEntity;

                activities.Add(new CrmActivity
                {
                    Type = type,
                    Subject = subject,
                    OccurredAt = now,
                    UserId = _currentUser.UserId,
                    CustomerId = customerId,
                    LeadId = leadId,
                    DealId = dealId,
                    TenantId = triggeredBy?.TenantId ?? _currentTenant.TenantId,
                    CreatedAt = now
                });
            }
        }

        return activities;
    }

    private (string? email, Guid? userId) GetAuditContext() =>
        (_currentUser.Email, _currentUser.UserId);

    private List<Notification> BuildNotifications()
    {
        var notifications = new List<Notification>();
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Notification) continue;

            string? title = null;
            string? message = null;
            string? url = null;
            string? kind = null;
            Guid? recipientId = null;

            switch (entry.Entity)
            {
                case TaskItem task when entry.State == EntityState.Added && task.AssignedToUserId.HasValue:
                    title = $"New task: {task.Title}";
                    message = task.DueDate.HasValue
                        ? $"Due {task.DueDate.Value.ToLocalTime():MMM d}"
                        : "No due date";
                    url = $"/tasks/{task.Id}";
                    kind = "task";
                    recipientId = task.AssignedToUserId;
                    break;

                case TaskItem task when entry.State == EntityState.Modified && task.AssignedToUserId.HasValue:
                    var assignProp = entry.Property(nameof(TaskItem.AssignedToUserId));
                    if (assignProp.IsModified && assignProp.OriginalValue?.ToString() != assignProp.CurrentValue?.ToString())
                    {
                        title = $"Task assigned: {task.Title}";
                        url = $"/tasks/{task.Id}";
                        kind = "task";
                        recipientId = task.AssignedToUserId;
                    }
                    break;

                case Deal deal when entry.State == EntityState.Modified && deal.AssignedToUserId.HasValue:
                    var dealStatusProp = entry.Property(nameof(Deal.Status));
                    if (dealStatusProp.IsModified)
                    {
                        var newStatus = dealStatusProp.CurrentValue?.ToString();

                        if (newStatus == "Won")
                        {
                            title = $"Deal won: {deal.Name}";
                            message = $"{deal.Value:C0}";
                            url = $"/deals/{deal.Id}";
                            kind = "deal";
                            recipientId = deal.AssignedToUserId;
                        }
                        else if (newStatus == "Lost")
                        {
                            title = $"Deal lost: {deal.Name}";
                            url = $"/deals/{deal.Id}";
                            kind = "deal";
                            recipientId = deal.AssignedToUserId;
                        }
                    }
                    break;

                case Lead lead when entry.State == EntityState.Modified && lead.AssignedToUserId.HasValue:
                    var leadStatusProp = entry.Property(nameof(Lead.Status));
                    if (leadStatusProp.IsModified && leadStatusProp.CurrentValue?.ToString() == "Converted")
                    {
                        title = $"Lead converted: {lead.FirstName} {lead.LastName}";
                        url = $"/leads/{lead.Id}";
                        kind = "lead";
                        recipientId = lead.AssignedToUserId;
                    }
                    break;
            }

            if (title is not null && recipientId.HasValue)
            {
                var triggeredBy = entry.Entity as TenantEntity;

                notifications.Add(new Notification
                {
                    TenantId = triggeredBy?.TenantId ?? _currentTenant.TenantId,
                    UserId = recipientId.Value,
                    Title = title,
                    Message = message,
                    Url = url,
                    Kind = kind,
                    IsRead = false,
                    CreatedAt = now
                });
            }
        }

        return notifications;
    }
}
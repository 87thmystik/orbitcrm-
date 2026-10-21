using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrbitCRM.Domain.Entities;

namespace OrbitCRM.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(n => n.Message)
            .HasMaxLength(1000);

        builder.Property(n => n.Url)
            .HasMaxLength(500);

        builder.Property(n => n.Kind)
            .HasMaxLength(50);

        builder.HasIndex(n => new { n.TenantId, n.UserId, n.IsRead });
        builder.HasIndex(n => new { n.TenantId, n.UserId, n.CreatedAt });
    }
}
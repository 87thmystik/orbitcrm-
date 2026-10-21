using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrbitCRM.Domain.Entities;

namespace OrbitCRM.Infrastructure.Data.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(l => l.Phone)
            .HasMaxLength(50);

        builder.Property(l => l.CompanyName)
            .HasMaxLength(200);

        builder.Property(l => l.Source)
            .HasMaxLength(100);

        builder.Property(l => l.EstimatedValue)
            .HasPrecision(18, 2);

        builder.Property(l => l.Notes)
            .HasMaxLength(4000);

        builder.HasIndex(l => l.TenantId);
        builder.HasIndex(l => new { l.TenantId, l.Status });
    }
}
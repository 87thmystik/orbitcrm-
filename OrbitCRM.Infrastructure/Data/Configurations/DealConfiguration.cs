using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrbitCRM.Domain.Entities;

namespace OrbitCRM.Infrastructure.Data.Configurations;

public class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("Deals");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Description)
            .HasMaxLength(2000);

        builder.Property(d => d.Value)
            .HasPrecision(18, 2);

        builder.HasOne(d => d.Lead)
            .WithMany()
            .HasForeignKey(d => d.LeadId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.Customer)
            .WithMany()
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.PipelineStage)
            .WithMany()
            .HasForeignKey(d => d.PipelineStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => new { d.TenantId, d.Status });
        builder.HasIndex(d => new { d.TenantId, d.PipelineStageId });
    }
}
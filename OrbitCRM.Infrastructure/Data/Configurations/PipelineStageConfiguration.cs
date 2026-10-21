using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrbitCRM.Domain.Entities;

namespace OrbitCRM.Infrastructure.Data.Configurations;

public class PipelineStageConfiguration : IEntityTypeConfiguration<PipelineStage>
{
    public void Configure(EntityTypeBuilder<PipelineStage> builder)
    {
        builder.ToTable("PipelineStages");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Color)
            .HasMaxLength(20);

        builder.HasIndex(p => new { p.TenantId, p.DisplayOrder });
    }
}
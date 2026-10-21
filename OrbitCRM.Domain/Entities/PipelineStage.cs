using OrbitCRM.Domain.Common;

namespace OrbitCRM.Domain.Entities;

public class PipelineStage : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int Probability { get; set; }
    public string? Color { get; set; }
    public bool IsDefault { get; set; }
}
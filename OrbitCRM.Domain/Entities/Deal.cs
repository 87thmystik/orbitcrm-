using OrbitCRM.Domain.Common;
using OrbitCRM.Domain.Enums;

namespace OrbitCRM.Domain.Entities;

public class Deal : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Value { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public DealStatus Status { get; set; } = DealStatus.Open;

    public Guid? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid PipelineStageId { get; set; }
    public PipelineStage PipelineStage { get; set; } = null!;

    public Guid? AssignedToUserId { get; set; }
}
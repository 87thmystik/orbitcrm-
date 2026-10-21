using OrbitCRM.Domain.Enums;

namespace OrbitCRM.Application.Deals.DTOs;

public class DealDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Value { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public DealStatus Status { get; set; }

    public Guid? LeadId { get; set; }
    public string? LeadName { get; set; }

    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }

    public Guid PipelineStageId { get; set; }
    public string PipelineStageName { get; set; } = string.Empty;
    public string? PipelineStageColor { get; set; }
    public int PipelineStageProbability { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateDealDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Value { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid PipelineStageId { get; set; }
}

public class UpdateDealDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Value { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public DealStatus Status { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid PipelineStageId { get; set; }
}

public class PipelineStageDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int Probability { get; set; }
    public string? Color { get; set; }
}
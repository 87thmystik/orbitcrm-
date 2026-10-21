namespace OrbitCRM.Application.Settings.DTOs;

public class PipelineStageSettingsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int Probability { get; set; }
    public string? Color { get; set; }
    public int DealCount { get; set; }
}

public class CreatePipelineStageDto
{
    public string Name { get; set; } = string.Empty;
    public int Probability { get; set; } = 50;
    public string Color { get; set; } = "#6b7280";
    public int DisplayOrder { get; set; }
}

public class UpdatePipelineStageDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Probability { get; set; }
    public string Color { get; set; } = "#6b7280";
}
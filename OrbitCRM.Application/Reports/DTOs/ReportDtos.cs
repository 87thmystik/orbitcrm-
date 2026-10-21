namespace OrbitCRM.Application.Reports.DTOs;

public class MonthlyValueDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    public decimal Value { get; set; }
    public int Count { get; set; }
}

public class SourceBreakdownDto
{
    public string Source { get; set; } = "Unknown";
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}

public class FunnelStageDto
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class StageBreakdownDto
{
    public Guid StageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int DealCount { get; set; }
    public decimal TotalValue { get; set; }
    public decimal WonValue { get; set; }
    public decimal LostValue { get; set; }
}

public class UserPerformanceDto
{
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = "Unassigned";
    public int DealsWon { get; set; }
    public int DealsLost { get; set; }
    public decimal Revenue { get; set; }
    public int TotalDeals { get; set; }
}

public class DateRangeDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}
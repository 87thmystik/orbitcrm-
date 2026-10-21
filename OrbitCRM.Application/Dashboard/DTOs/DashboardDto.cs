using OrbitCRM.Application.Activities.DTOs;
using OrbitCRM.Application.Deals.DTOs;
using OrbitCRM.Application.Tasks.DTOs;

namespace OrbitCRM.Application.Dashboard.DTOs;

public class DashboardSummaryDto
{
    // KPI Cards
    public int TotalCustomers { get; set; }
    public int ActiveLeads { get; set; }
    public int OpenDeals { get; set; }
    public decimal PipelineValue { get; set; }
    public decimal WonRevenue { get; set; }
    public int OpenTasks { get; set; }
    public int OverdueTasks { get; set; }

    // Pipeline breakdown
    public List<PipelineStageSummary> PipelineByStage { get; set; } = new();

    // Today's activities
    public List<ActivityDto> TodaysActivities { get; set; } = new();

    // Recent deals
    public List<DealDto> RecentDeals { get; set; } = new();

    // Overdue tasks
    public List<TaskDto> OverdueTaskList { get; set; } = new();
}

public class PipelineStageSummary
{
    public Guid StageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int DealCount { get; set; }
    public decimal TotalValue { get; set; }
    public int Probability { get; set; }
}
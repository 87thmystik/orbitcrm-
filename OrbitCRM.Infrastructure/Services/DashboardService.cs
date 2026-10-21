using Microsoft.EntityFrameworkCore;
using OrbitCRM.Application.Activities.DTOs;
using OrbitCRM.Application.Dashboard.DTOs;
using OrbitCRM.Application.Dashboard.Interfaces;
using OrbitCRM.Application.Deals.DTOs;
using OrbitCRM.Application.Tasks.DTOs;
using OrbitCRM.Domain.Enums;
using OrbitCRM.Infrastructure.Data;
using OrbitCRM.Infrastructure.Identity;

namespace OrbitCRM.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly OrbitCRMDbContext _db;
    private readonly TenantGuard _tenantGuard;

    public DashboardService(OrbitCRMDbContext db, TenantGuard tenantGuard)
    {
        _db = db;
        _tenantGuard = tenantGuard;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var summary = new DashboardSummaryDto();

        // --- KPI cards ---
        summary.TotalCustomers = await _db.Customers.CountAsync(cancellationToken);

        summary.ActiveLeads = await _db.Leads
            .CountAsync(l => l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost, cancellationToken);

        summary.OpenDeals = await _db.Deals
            .CountAsync(d => d.Status == DealStatus.Open, cancellationToken);

        summary.PipelineValue = await _db.Deals
            .Where(d => d.Status == DealStatus.Open)
            .SumAsync(d => (decimal?)d.Value, cancellationToken) ?? 0m;

        summary.WonRevenue = await _db.Deals
            .Where(d => d.Status == DealStatus.Won)
            .SumAsync(d => (decimal?)d.Value, cancellationToken) ?? 0m;

        summary.OpenTasks = await _db.Tasks
            .CountAsync(t => t.Status != TaskItemStatus.Completed && t.Status != TaskItemStatus.Cancelled, cancellationToken);

        summary.OverdueTasks = await _db.Tasks
            .CountAsync(t =>
                t.DueDate.HasValue &&
                t.DueDate.Value < today &&
                t.Status != TaskItemStatus.Completed &&
                t.Status != TaskItemStatus.Cancelled, cancellationToken);

        // --- Pipeline by stage ---
        var stages = await _db.PipelineStages
            .AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);

        var dealGroups = await _db.Deals
            .Where(d => d.Status == DealStatus.Open)
            .GroupBy(d => d.PipelineStageId)
            .Select(g => new
            {
                StageId = g.Key,
                Count = g.Count(),
                Value = g.Sum(x => x.Value)
            })
            .ToListAsync(cancellationToken);

        summary.PipelineByStage = stages.Select(s =>
        {
            var group = dealGroups.FirstOrDefault(g => g.StageId == s.Id);
            return new PipelineStageSummary
            {
                StageId = s.Id,
                StageName = s.Name,
                Color = s.Color,
                DealCount = group?.Count ?? 0,
                TotalValue = group?.Value ?? 0m,
                Probability = s.Probability
            };
        }).ToList();

        // --- Today's activities ---
        summary.TodaysActivities = await _db.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.Deal)
            .Where(a => a.OccurredAt >= today && a.OccurredAt < tomorrow)
            .OrderByDescending(a => a.OccurredAt)
            .Take(10)
            .Select(a => new ActivityDto
            {
                Id = a.Id,
                Type = a.Type,
                Subject = a.Subject,
                Description = a.Description,
                OccurredAt = a.OccurredAt,
                UserId = a.UserId,
                CustomerId = a.CustomerId,
                CustomerName = a.Customer != null ? a.Customer.FirstName + " " + a.Customer.LastName : null,
                LeadId = a.LeadId,
                LeadName = a.Lead != null ? a.Lead.FirstName + " " + a.Lead.LastName : null,
                DealId = a.DealId,
                DealName = a.Deal != null ? a.Deal.Name : null,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // --- Recent deals ---
        summary.RecentDeals = await _db.Deals
            .AsNoTracking()
            .Include(d => d.PipelineStage)
            .Include(d => d.Customer)
            .Include(d => d.Lead)
            .Where(d => d.Status == DealStatus.Open)
            .OrderByDescending(d => d.CreatedAt)
            .Take(5)
            .Select(d => new DealDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                Value = d.Value,
                ExpectedCloseDate = d.ExpectedCloseDate,
                Status = d.Status,
                LeadId = d.LeadId,
                LeadName = d.Lead != null ? d.Lead.FirstName + " " + d.Lead.LastName : null,
                CustomerId = d.CustomerId,
                CustomerName = d.Customer != null ? d.Customer.FirstName + " " + d.Customer.LastName : null,
                PipelineStageId = d.PipelineStageId,
                PipelineStageName = d.PipelineStage.Name,
                PipelineStageColor = d.PipelineStage.Color,
                PipelineStageProbability = d.PipelineStage.Probability,
                AssignedToUserId = d.AssignedToUserId,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        // --- Overdue tasks ---
        summary.OverdueTaskList = await _db.Tasks
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.Lead)
            .Include(t => t.Deal)
            .Where(t =>
                t.DueDate.HasValue &&
                t.DueDate.Value < today &&
                t.Status != TaskItemStatus.Completed &&
                t.Status != TaskItemStatus.Cancelled)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                DueDate = t.DueDate,
                Priority = t.Priority,
                Status = t.Status,
                CompletedAt = t.CompletedAt,
                CustomerId = t.CustomerId,
                CustomerName = t.Customer != null ? t.Customer.FirstName + " " + t.Customer.LastName : null,
                LeadId = t.LeadId,
                LeadName = t.Lead != null ? t.Lead.FirstName + " " + t.Lead.LastName : null,
                DealId = t.DealId,
                DealName = t.Deal != null ? t.Deal.Name : null,
                AssignedToUserId = t.AssignedToUserId,
                CreatedByUserId = t.CreatedByUserId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return summary;
    }
}
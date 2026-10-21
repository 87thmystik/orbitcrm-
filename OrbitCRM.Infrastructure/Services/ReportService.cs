using Microsoft.EntityFrameworkCore;
using OrbitCRM.Application.Reports.DTOs;
using OrbitCRM.Application.Reports.Interfaces;
using OrbitCRM.Domain.Enums;
using OrbitCRM.Infrastructure.Data;
using OrbitCRM.Infrastructure.Identity;

namespace OrbitCRM.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly OrbitCRMDbContext _db;
    private readonly TenantGuard _tenantGuard;

    public ReportService(OrbitCRMDbContext db, TenantGuard tenantGuard)
    {
        _db = db;
        _tenantGuard = tenantGuard;
    }

    public async Task<List<MonthlyValueDto>> GetWonRevenueByMonthAsync(DateRangeDto range, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var raw = await _db.Deals
            .Where(d => d.Status == DealStatus.Won)
            .Where(d => d.ExpectedCloseDate.HasValue)
            .Where(d => d.ExpectedCloseDate!.Value >= range.Start && d.ExpectedCloseDate.Value < range.End)
            .GroupBy(d => new { d.ExpectedCloseDate!.Value.Year, d.ExpectedCloseDate.Value.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Value = g.Sum(x => x.Value),
                Count = g.Count()
            })
            .ToListAsync(ct);

        return FillMissingMonths(range, raw.Select(r => new MonthlyValueDto
        {
            Year = r.Year,
            Month = r.Month,
            Value = r.Value,
            Count = r.Count
        }).ToList());
    }

    public async Task<List<MonthlyValueDto>> GetCustomerGrowthByMonthAsync(DateRangeDto range, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var raw = await _db.Customers
            .Where(c => c.CreatedAt >= range.Start && c.CreatedAt < range.End)
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .ToListAsync(ct);

        return FillMissingMonths(range, raw.Select(r => new MonthlyValueDto
        {
            Year = r.Year,
            Month = r.Month,
            Value = 0,
            Count = r.Count
        }).ToList());
    }

    public async Task<List<SourceBreakdownDto>> GetLeadsBySourceAsync(DateRangeDto range, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        return await _db.Leads
            .Where(l => l.CreatedAt >= range.Start && l.CreatedAt < range.End)
            .GroupBy(l => l.Source ?? "Unknown")
            .Select(g => new SourceBreakdownDto
            {
                Source = g.Key,
                Count = g.Count(),
                TotalValue = g.Sum(x => x.EstimatedValue)
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);
    }

    public async Task<List<FunnelStageDto>> GetLeadFunnelAsync(DateRangeDto range, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var total = await _db.Leads
            .Where(l => l.CreatedAt >= range.Start && l.CreatedAt < range.End)
            .CountAsync(ct);

        if (total == 0) return new List<FunnelStageDto>();

        var grouped = await _db.Leads
            .Where(l => l.CreatedAt >= range.Start && l.CreatedAt < range.End)
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Funnel order — cumulative from top
        var order = new[]
        {
            LeadStatus.New,
            LeadStatus.Contacted,
            LeadStatus.Qualified,
            LeadStatus.Converted
        };

        var result = new List<FunnelStageDto>();

        // For funnel, each stage = leads that reached AT LEAST this stage
        // For simplicity, count the current statuses cumulatively:
        var statusCount = grouped.ToDictionary(x => x.Status, x => x.Count);

        int cumulative = total;
        foreach (var status in order)
        {
            result.Add(new FunnelStageDto
            {
                Stage = status.ToString(),
                Count = cumulative,
                Percentage = total > 0 ? Math.Round((double)cumulative / total * 100, 1) : 0
            });

            // Subtracting the ones lost at this stage for next step
            var lostAtStage = statusCount.GetValueOrDefault(status, 0);
            cumulative -= lostAtStage;

            if (cumulative < 0) cumulative = 0;
        }

        return result;
    }

    public async Task<List<StageBreakdownDto>> GetDealsByPipelineStageAsync(DateRangeDto range, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var stages = await _db.PipelineStages
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new { s.Id, s.Name, s.Color })
            .ToListAsync(ct);

        var deals = await _db.Deals
            .Where(d => d.CreatedAt >= range.Start && d.CreatedAt < range.End)
            .Select(d => new { d.PipelineStageId, d.Status, d.Value })
            .ToListAsync(ct);

        return stages.Select(s =>
        {
            var stageDeals = deals.Where(d => d.PipelineStageId == s.Id).ToList();
            return new StageBreakdownDto
            {
                StageId = s.Id,
                StageName = s.Name,
                Color = s.Color,
                DealCount = stageDeals.Count,
                TotalValue = stageDeals.Sum(d => d.Value),
                WonValue = stageDeals.Where(d => d.Status == DealStatus.Won).Sum(d => d.Value),
                LostValue = stageDeals.Where(d => d.Status == DealStatus.Lost).Sum(d => d.Value)
            };
        }).ToList();
    }

    public async Task<List<UserPerformanceDto>> GetRevenueBySalespersonAsync(DateRangeDto range, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var deals = await _db.Deals
            .Where(d => d.CreatedAt >= range.Start && d.CreatedAt < range.End)
            .Select(d => new { d.AssignedToUserId, d.Status, d.Value })
            .ToListAsync(ct);

        var userIds = deals.Where(d => d.AssignedToUserId.HasValue)
            .Select(d => d.AssignedToUserId!.Value)
            .Distinct()
            .ToList();

        var userNames = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .ToListAsync(ct);

        var groups = deals.GroupBy(d => d.AssignedToUserId);

        return groups.Select(g =>
        {
            var userId = g.Key;
            var name = userId is null
                ? "Unassigned"
                : userNames.FirstOrDefault(u => u.Id == userId) is { } u
                    ? $"{u.FirstName} {u.LastName}".Trim()
                    : "Unknown";

            return new UserPerformanceDto
            {
                UserId = userId,
                UserName = name,
                DealsWon = g.Count(d => d.Status == DealStatus.Won),
                DealsLost = g.Count(d => d.Status == DealStatus.Lost),
                Revenue = g.Where(d => d.Status == DealStatus.Won).Sum(d => d.Value),
                TotalDeals = g.Count()
            };
        })
        .OrderByDescending(u => u.Revenue)
        .ToList();
    }

    // Helper — fills months with zero values so charts look continuous
    private static List<MonthlyValueDto> FillMissingMonths(DateRangeDto range, List<MonthlyValueDto> actual)
    {
        var result = new List<MonthlyValueDto>();
        var cursor = new DateTime(range.Start.Year, range.Start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endMonth = new DateTime(range.End.Year, range.End.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (cursor <= endMonth)
        {
            var existing = actual.FirstOrDefault(a => a.Year == cursor.Year && a.Month == cursor.Month);

            result.Add(new MonthlyValueDto
            {
                Year = cursor.Year,
                Month = cursor.Month,
                Value = existing?.Value ?? 0,
                Count = existing?.Count ?? 0
            });

            cursor = cursor.AddMonths(1);
        }

        return result;
    }
}
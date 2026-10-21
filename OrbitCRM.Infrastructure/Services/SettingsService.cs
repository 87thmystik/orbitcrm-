using Microsoft.EntityFrameworkCore;
using OrbitCRM.Application.Common.Models;
using OrbitCRM.Application.Settings.DTOs;
using OrbitCRM.Application.Settings.Interfaces;
using OrbitCRM.Domain.Entities;
using OrbitCRM.Infrastructure.Data;
using OrbitCRM.Infrastructure.Identity;

namespace OrbitCRM.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly OrbitCRMDbContext _db;
    private readonly CurrentTenant _currentTenant;
    private readonly TenantGuard _tenantGuard;

    public SettingsService(
        OrbitCRMDbContext db,
        CurrentTenant currentTenant,
        TenantGuard tenantGuard)
    {
        _db = db;
        _currentTenant = currentTenant;
        _tenantGuard = tenantGuard;
    }

    // ---------------------------------------------------------
    // Tenant
    // ---------------------------------------------------------

    public async Task<TenantSettingsDto?> GetTenantSettingsAsync(CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var tenantId = _currentTenant.TenantId;
        if (tenantId == Guid.Empty) return null;

        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null) return null;

        return new TenantSettingsDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            Email = tenant.Email,
            Phone = tenant.Phone,
            CreatedAt = tenant.CreatedAt,
            IsActive = tenant.IsActive
        };
    }

    public async Task<Result> UpdateTenantSettingsAsync(UpdateTenantSettingsDto dto, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var tenantId = _currentTenant.TenantId;
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null) return Result.Failure("Tenant not found.");

        var name = dto.Name.Trim();
        var slug = dto.Slug.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Company name is required.");

        if (string.IsNullOrWhiteSpace(slug))
            return Result.Failure("Slug is required.");

        if (!IsValidSlug(slug))
            return Result.Failure("Slug can only contain lowercase letters, numbers, and dashes.");

        // Ensure slug is unique across tenants
        var slugTaken = await _db.Tenants
            .AnyAsync(t => t.Id != tenantId && t.Slug == slug, ct);

        if (slugTaken)
            return Result.Failure("Another company is already using that slug.");

        tenant.Name = name;
        tenant.Slug = slug;
        tenant.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        tenant.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---------------------------------------------------------
    // Pipeline Stages
    // ---------------------------------------------------------

    public async Task<List<PipelineStageSettingsDto>> GetPipelineStagesAsync(CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var stages = await _db.PipelineStages
            .AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        var counts = await _db.Deals
            .Where(d => !d.IsDeleted)
            .GroupBy(d => d.PipelineStageId)
            .Select(g => new { StageId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return stages.Select(s => new PipelineStageSettingsDto
        {
            Id = s.Id,
            Name = s.Name,
            DisplayOrder = s.DisplayOrder,
            Probability = s.Probability,
            Color = s.Color,
            DealCount = counts.FirstOrDefault(c => c.StageId == s.Id)?.Count ?? 0
        }).ToList();
    }

    public async Task<Result<Guid>> CreatePipelineStageAsync(CreatePipelineStageDto dto, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Guid>("Stage name is required.");

        // Default display order = max + 1
        var maxOrder = await _db.PipelineStages
            .Select(s => (int?)s.DisplayOrder)
            .MaxAsync(ct) ?? 0;

        var stage = new PipelineStage
        {
            Name = name,
            DisplayOrder = maxOrder + 1,
            Probability = Math.Clamp(dto.Probability, 0, 100),
            Color = string.IsNullOrWhiteSpace(dto.Color) ? "#6b7280" : dto.Color
        };

        _db.PipelineStages.Add(stage);
        await _db.SaveChangesAsync(ct);

        return Result.Success(stage.Id);
    }

    public async Task<Result> UpdatePipelineStageAsync(UpdatePipelineStageDto dto, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var stage = await _db.PipelineStages.FirstOrDefaultAsync(s => s.Id == dto.Id, ct);
        if (stage is null) return Result.Failure("Stage not found.");

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Stage name is required.");

        stage.Name = name;
        stage.Probability = Math.Clamp(dto.Probability, 0, 100);
        stage.Color = string.IsNullOrWhiteSpace(dto.Color) ? "#6b7280" : dto.Color;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeletePipelineStageAsync(Guid id, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var stage = await _db.PipelineStages.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (stage is null) return Result.Failure("Stage not found.");

        // Guard: cannot delete if deals reference it
        var dealCount = await _db.Deals.CountAsync(d => d.PipelineStageId == id, ct);
        if (dealCount > 0)
            return Result.Failure($"Cannot delete — {dealCount} deal(s) are still using this stage. Move them to another stage first.");

        // Guard: must have at least 2 stages
        var stageCount = await _db.PipelineStages.CountAsync(ct);
        if (stageCount <= 2)
            return Result.Failure("Cannot delete — a pipeline needs at least 2 stages.");

        _db.PipelineStages.Remove(stage);
        await _db.SaveChangesAsync(ct);

        // Reorder remaining stages
        await ResequenceStagesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> MovePipelineStageAsync(Guid id, int direction, CancellationToken ct = default)
    {
        // direction: -1 = move up, +1 = move down
        await _tenantGuard.EnsureAsync();

        if (direction != -1 && direction != 1)
            return Result.Failure("Invalid direction.");

        var stages = await _db.PipelineStages
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        var index = stages.FindIndex(s => s.Id == id);
        if (index == -1) return Result.Failure("Stage not found.");

        var targetIndex = index + direction;
        if (targetIndex < 0 || targetIndex >= stages.Count)
            return Result.Success(); // already at boundary, no-op

        // Swap display orders
        var temp = stages[index].DisplayOrder;
        stages[index].DisplayOrder = stages[targetIndex].DisplayOrder;
        stages[targetIndex].DisplayOrder = temp;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------

    private async Task ResequenceStagesAsync(CancellationToken ct)
    {
        var stages = await _db.PipelineStages
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        for (int i = 0; i < stages.Count; i++)
        {
            stages[i].DisplayOrder = i + 1;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static bool IsValidSlug(string slug)
    {
        foreach (var c in slug)
        {
            if (!char.IsLetterOrDigit(c) && c != '-') return false;
        }
        return true;
    }
}
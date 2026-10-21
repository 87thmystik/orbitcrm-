using Microsoft.EntityFrameworkCore;
using OrbitCRM.Application.Common.Models;
using OrbitCRM.Application.Deals.DTOs;
using OrbitCRM.Application.Deals.Interfaces;
using OrbitCRM.Domain.Entities;
using OrbitCRM.Domain.Enums;
using OrbitCRM.Infrastructure.Data;
using OrbitCRM.Infrastructure.Identity;

namespace OrbitCRM.Infrastructure.Services;

public class DealService : IDealService
{
    private readonly OrbitCRMDbContext _db;
    private readonly TenantGuard _tenantGuard;

    public DealService(OrbitCRMDbContext db, TenantGuard tenantGuard)
    {
        _db = db;
        _tenantGuard = tenantGuard;
    }

    public async Task<List<DealDto>> GetDealsForCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();
        return await _db.Deals
            .AsNoTracking()
            .Include(d => d.PipelineStage)
            .Where(d => d.CustomerId == customerId && d.Status == DealStatus.Open) // Only open deals for the customer view
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DealDto
            {
                Id = d.Id,
                Name = d.Name,
                Value = d.Value,
                Status = d.Status,
                PipelineStageName = d.PipelineStage.Name,
                PipelineStageColor = d.PipelineStage.Color,
                ExpectedCloseDate = d.ExpectedCloseDate
            })
            .ToListAsync(ct);
    }

    public async Task<PagedResult<DealDto>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        Guid? pipelineStageId = null,
        CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Deals
            .AsNoTracking()
            .Include(d => d.PipelineStage)
            .Include(d => d.Customer)
            .Include(d => d.Lead)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                EF.Functions.ILike(d.Name, $"%{term}%") ||
                (d.Description != null && EF.Functions.ILike(d.Description, $"%{term}%")));
        }

        if (pipelineStageId.HasValue)
        {
            query = query.Where(d => d.PipelineStageId == pipelineStageId.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return new PagedResult<DealDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DealDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        return await _db.Deals
            .AsNoTracking()
            .Include(d => d.PipelineStage)
            .Include(d => d.Customer)
            .Include(d => d.Lead)
            .Where(d => d.Id == id)
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
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<Guid>> CreateAsync(CreateDealDto dto, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        // Validate stage belongs to this tenant
        var stageExists = await _db.PipelineStages
            .AnyAsync(s => s.Id == dto.PipelineStageId, cancellationToken);

        if (!stageExists)
        {
            return Result.Failure<Guid>("Invalid pipeline stage.");
        }

        var deal = new Deal
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            Value = dto.Value,
            ExpectedCloseDate = dto.ExpectedCloseDate.HasValue
                                ? DateTime.SpecifyKind(dto.ExpectedCloseDate.Value, DateTimeKind.Utc)
                                : null,
            Status = DealStatus.Open,
            LeadId = dto.LeadId,
            CustomerId = dto.CustomerId,
            PipelineStageId = dto.PipelineStageId
        };

        _db.Deals.Add(deal);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(deal.Id);
    }

    public async Task<Result> UpdateAsync(UpdateDealDto dto, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var deal = await _db.Deals
            .FirstOrDefaultAsync(d => d.Id == dto.Id, cancellationToken);

        if (deal is null) return Result.Failure("Deal not found.");

        var stageExists = await _db.PipelineStages
            .AnyAsync(s => s.Id == dto.PipelineStageId, cancellationToken);

        if (!stageExists) return Result.Failure("Invalid pipeline stage.");

        deal.Name = dto.Name.Trim();
        deal.Description = dto.Description?.Trim();
        deal.Value = dto.Value;
        deal.ExpectedCloseDate = dto.ExpectedCloseDate.HasValue
                                ? DateTime.SpecifyKind(dto.ExpectedCloseDate.Value, DateTimeKind.Utc)
                                : null;
        deal.Status = dto.Status;
        deal.LeadId = dto.LeadId;
        deal.CustomerId = dto.CustomerId;
        deal.PipelineStageId = dto.PipelineStageId;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var deal = await _db.Deals
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (deal is null) return Result.Failure("Deal not found.");

        deal.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkAsWonAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var deal = await _db.Deals
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (deal is null) return Result.Failure("Deal not found.");

        var wonStage = await _db.PipelineStages
            .OrderByDescending(s => s.Probability)
            .FirstOrDefaultAsync(cancellationToken);

        if (wonStage is null) return Result.Failure("No pipeline stages configured.");

        deal.Status = DealStatus.Won;
        deal.PipelineStageId = wonStage.Id;
        deal.ExpectedCloseDate = deal.ExpectedCloseDate ?? DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> MarkAsLostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var deal = await _db.Deals
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (deal is null) return Result.Failure("Deal not found.");

        var lostStage = await _db.PipelineStages
            .OrderBy(s => s.Probability)
            .FirstOrDefaultAsync(cancellationToken);

        if (lostStage is null) return Result.Failure("No pipeline stages configured.");

        deal.Status = DealStatus.Lost;
        deal.PipelineStageId = lostStage.Id;
        deal.ExpectedCloseDate = deal.ExpectedCloseDate ?? DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<List<PipelineStageDto>> GetPipelineStagesAsync(CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        return await _db.PipelineStages
            .AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new PipelineStageDto
            {
                Id = s.Id,
                Name = s.Name,
                DisplayOrder = s.DisplayOrder,
                Probability = s.Probability,
                Color = s.Color
            })
            .ToListAsync(cancellationToken);
    }
}
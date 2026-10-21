using Microsoft.EntityFrameworkCore;
using OrbitCRM.Application.Common.Models;
using OrbitCRM.Application.Leads.DTOs;
using OrbitCRM.Application.Leads.Interfaces;
using OrbitCRM.Domain.Entities;
using OrbitCRM.Domain.Enums;
using OrbitCRM.Infrastructure.Data;
using OrbitCRM.Infrastructure.Identity;

namespace OrbitCRM.Infrastructure.Services;

public class LeadService : ILeadService
{
    private readonly OrbitCRMDbContext _db;
    private readonly TenantGuard _tenantGuard;

    public LeadService(OrbitCRMDbContext db, TenantGuard tenantGuard)
    {
        _db = db;
        _tenantGuard = tenantGuard;
    }

    public async Task<PagedResult<LeadDto>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        LeadStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Leads.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l =>
                EF.Functions.ILike(l.FirstName, $"%{term}%") ||
                EF.Functions.ILike(l.LastName, $"%{term}%") ||
                EF.Functions.ILike(l.Email, $"%{term}%") ||
                (l.CompanyName != null && EF.Functions.ILike(l.CompanyName, $"%{term}%")));
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LeadDto
            {
                Id = l.Id,
                FirstName = l.FirstName,
                LastName = l.LastName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                EstimatedValue = l.EstimatedValue,
                AssignedToUserId = l.AssignedToUserId,
                Notes = l.Notes,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LeadDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<LeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        return await _db.Leads
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new LeadDto
            {
                Id = l.Id,
                FirstName = l.FirstName,
                LastName = l.LastName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                EstimatedValue = l.EstimatedValue,
                AssignedToUserId = l.AssignedToUserId,
                Notes = l.Notes,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<Guid>> CreateAsync(CreateLeadDto dto, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var lead = new Lead
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Phone = dto.Phone?.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Source = dto.Source?.Trim(),
            Status = LeadStatus.New,
            Priority = dto.Priority,
            EstimatedValue = dto.EstimatedValue,
            AssignedToUserId = dto.AssignedToUserId,
            Notes = dto.Notes?.Trim()
        };

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(lead.Id);
    }

    public async Task<Result> UpdateAsync(UpdateLeadDto dto, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == dto.Id, cancellationToken);

        if (lead is null)
        {
            return Result.Failure("Lead not found.");
        }

        lead.FirstName = dto.FirstName.Trim();
        lead.LastName = dto.LastName.Trim();
        lead.Email = dto.Email.Trim().ToLowerInvariant();
        lead.Phone = dto.Phone?.Trim();
        lead.CompanyName = dto.CompanyName?.Trim();
        lead.Source = dto.Source?.Trim();
        lead.Status = dto.Status;
        lead.Priority = dto.Priority;
        lead.EstimatedValue = dto.EstimatedValue;
        lead.AssignedToUserId = dto.AssignedToUserId;
        lead.Notes = dto.Notes?.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (lead is null)
        {
            return Result.Failure("Lead not found.");
        }

        lead.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<Guid>> ConvertToCustomerAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);

        if (lead is null)
        {
            return Result.Failure<Guid>("Lead not found.");
        }

        if (lead.Status == LeadStatus.Converted)
        {
            return Result.Failure<Guid>("Lead has already been converted.");
        }

        // Check for a duplicate customer with the same email
        var existingCustomer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Email == lead.Email, cancellationToken);

        if (existingCustomer is not null)
        {
            return Result.Failure<Guid>("A customer with this email already exists.");
        }

        var customer = new Customer
        {
            FirstName = lead.FirstName,
            LastName = lead.LastName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Notes = lead.Notes
        };

        _db.Customers.Add(customer);

        lead.Status = LeadStatus.Converted;
        lead.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(customer.Id);
    }
}
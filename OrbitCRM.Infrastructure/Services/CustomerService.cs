using Microsoft.EntityFrameworkCore;
using OrbitCRM.Application.Common.Models;
using OrbitCRM.Application.Customers.DTOs;
using OrbitCRM.Application.Customers.Interfaces;
using OrbitCRM.Domain.Entities;
using OrbitCRM.Infrastructure.Data;
using OrbitCRM.Infrastructure.Identity;

namespace OrbitCRM.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly OrbitCRMDbContext _db;
    private readonly TenantGuard _tenantGuard;

    public CustomerService(OrbitCRMDbContext db, TenantGuard tenantGuard)
    {
        _db = db;
        _tenantGuard = tenantGuard;
    }

    public async Task<PagedResult<CustomerDto>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                EF.Functions.ILike(c.FirstName, $"%{term}%") ||
                EF.Functions.ILike(c.LastName, $"%{term}%") ||
                EF.Functions.ILike(c.Email, $"%{term}%") ||
                (c.CompanyName != null && EF.Functions.ILike(c.CompanyName, $"%{term}%")));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                Notes = c.Notes,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        return await _db.Customers
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                Notes = c.Notes,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<Guid>> CreateAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var exists = await _db.Customers
            .AnyAsync(c => c.Email == dto.Email, cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>("A customer with this email already exists.");
        }

        var customer = new Customer
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Phone = dto.Phone?.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Address = dto.Address?.Trim(),
            Notes = dto.Notes?.Trim()
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(customer.Id);
    }

    public async Task<Result> UpdateAsync(UpdateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == dto.Id, cancellationToken);

        if (customer is null)
        {
            return Result.Failure("Customer not found.");
        }

        var emailTaken = await _db.Customers
            .AnyAsync(c => c.Id != dto.Id && c.Email == dto.Email, cancellationToken);

        if (emailTaken)
        {
            return Result.Failure("Another customer already uses this email.");
        }

        customer.FirstName = dto.FirstName.Trim();
        customer.LastName = dto.LastName.Trim();
        customer.Email = dto.Email.Trim().ToLowerInvariant();
        customer.Phone = dto.Phone?.Trim();
        customer.CompanyName = dto.CompanyName?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.Notes = dto.Notes?.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tenantGuard.EnsureAsync();

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null)
        {
            return Result.Failure("Customer not found.");
        }

        customer.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
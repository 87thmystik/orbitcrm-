using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrbitCRM.Application.Common.Models;
using OrbitCRM.Application.Users.DTOs;
using OrbitCRM.Application.Users.Interfaces;
using OrbitCRM.Domain.Entities;
using OrbitCRM.Domain.Identity;
using OrbitCRM.Infrastructure.Data;        // ← OrbitCRMDbContext lives here
using OrbitCRM.Infrastructure.Identity;    // ← CurrentTenant, TenantGuard

namespace OrbitCRM.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly OrbitCRMDbContext _db;
    private readonly TenantGuard _tenantGuard;
    private readonly CurrentTenant _currentTenant;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(
        OrbitCRMDbContext db,
        TenantGuard tenantGuard,
        CurrentTenant currentTenant,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _tenantGuard = tenantGuard;
        _currentTenant = currentTenant;
        _userManager = userManager;
    }

    public async Task<List<TenantUserDto>> GetTenantUsersAsync(CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();
        var tenantId = _currentTenant.TenantId;

        return await _db.TenantMemberships
            .Where(m => m.TenantId == tenantId)
            .Include(m => m.User)
            .OrderBy(m => m.User.FirstName)
            .Select(m => new TenantUserDto
            {
                UserId = m.UserId,
                Email = m.User.Email ?? "",
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                Role = m.Role,
                IsActive = m.IsActive,
                JoinedAt = m.JoinedAt
            })
            .ToListAsync(ct);
    }

    public async Task<Result<Guid>> CreateUserAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
            return Result.Failure<Guid>("A user with this email already exists.");

        if (!Roles.All().Contains(dto.Role))
            return Result.Failure<Guid>("Invalid role.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure<Guid>(errors);
        }

        _db.TenantMemberships.Add(new TenantMembership
        {
            TenantId = _currentTenant.TenantId,
            UserId = user.Id,
            Role = dto.Role,
            IsActive = true
        });
        await _db.SaveChangesAsync(ct);

        return Result.Success(user.Id);
    }

    public async Task<Result> UpdateUserRoleAsync(UpdateUserRoleDto dto, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        if (!Roles.All().Contains(dto.Role))
            return Result.Failure("Invalid role.");

        var membership = await _db.TenantMemberships
            .FirstOrDefaultAsync(m => m.UserId == dto.UserId && m.TenantId == _currentTenant.TenantId, ct);

        if (membership is null) return Result.Failure("User not found in this tenant.");

        if (membership.Role == Roles.Owner)
            return Result.Failure("The owner role cannot be changed.");

        membership.Role = dto.Role;
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        await _tenantGuard.EnsureAsync();

        var membership = await _db.TenantMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == _currentTenant.TenantId, ct);

        if (membership is null) return Result.Failure("User not found in this tenant.");

        if (membership.Role == Roles.Owner && !isActive)
            return Result.Failure("The owner cannot be deactivated.");

        membership.IsActive = isActive;
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
using OrbitCRM.Application.Common.Models;
using OrbitCRM.Application.Users.DTOs;

namespace OrbitCRM.Application.Users.Interfaces;

public interface IUserService
{
    Task<List<TenantUserDto>> GetTenantUsersAsync(CancellationToken ct = default);
    Task<Result<Guid>> CreateUserAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<Result> UpdateUserRoleAsync(UpdateUserRoleDto dto, CancellationToken ct = default);
    Task<Result> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);
}
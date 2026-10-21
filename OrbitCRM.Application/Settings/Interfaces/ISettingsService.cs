using OrbitCRM.Application.Common.Models;
using OrbitCRM.Application.Settings.DTOs;

namespace OrbitCRM.Application.Settings.Interfaces;

public interface ISettingsService
{
    // Tenant
    Task<TenantSettingsDto?> GetTenantSettingsAsync(CancellationToken ct = default);
    Task<Result> UpdateTenantSettingsAsync(UpdateTenantSettingsDto dto, CancellationToken ct = default);

    // Pipeline Stages
    Task<List<PipelineStageSettingsDto>> GetPipelineStagesAsync(CancellationToken ct = default);
    Task<Result<Guid>> CreatePipelineStageAsync(CreatePipelineStageDto dto, CancellationToken ct = default);
    Task<Result> UpdatePipelineStageAsync(UpdatePipelineStageDto dto, CancellationToken ct = default);
    Task<Result> DeletePipelineStageAsync(Guid id, CancellationToken ct = default);
    Task<Result> MovePipelineStageAsync(Guid id, int direction, CancellationToken ct = default);
}
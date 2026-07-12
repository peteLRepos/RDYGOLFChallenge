using GolfClub.Application.DTOs;

namespace GolfClub.Application.Services;

public interface IResourceService
{
    Task<List<ResourceDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    Task<ResourceDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ResourceDto> CreateAsync(CreateResourceRequest request, CancellationToken ct = default);
    Task<ResourceDto> UpdateAsync(Guid id, UpdateResourceRequest request, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
}

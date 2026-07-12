using GolfClub.Domain.Entities;

namespace GolfClub.Application.Interfaces;

public interface IResourceRepository
{
    Task<Resource?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Resource>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    Task AddAsync(Resource resource, CancellationToken ct = default);
}

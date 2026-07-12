using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GolfClub.Infrastructure.Persistence.Repositories;

public class ResourceRepository : IResourceRepository
{
    private readonly GolfClubDbContext _context;

    public ResourceRepository(GolfClubDbContext context)
    {
        _context = context;
    }

    public Task<Resource?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Resources.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<List<Resource>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = _context.Resources.AsQueryable();
        if (!includeInactive)
            query = query.Where(r => r.IsActive);

        return await query.OrderBy(r => r.Type).ThenBy(r => r.Name).ToListAsync(ct);
    }

    public async Task AddAsync(Resource resource, CancellationToken ct = default) =>
        await _context.Resources.AddAsync(resource, ct);

    public void Update(Resource resource) => _context.Resources.Update(resource);
}

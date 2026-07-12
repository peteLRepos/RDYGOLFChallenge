using GolfClub.Application.Interfaces;

namespace GolfClub.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly GolfClubDbContext _context;

    public UnitOfWork(GolfClubDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}

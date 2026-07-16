using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GolfClub.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly GolfClubDbContext _context;

    public UserRepository(GolfClubDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetTrackedByEmailAsync(string email, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<List<User>> SearchByNameAsync(string query, CancellationToken ct = default)
    {
        // Escape LIKE/ILIKE wildcard characters in the query itself (Postgres's default ILIKE
        // escape character is backslash) so a literal '%' or '_' in someone's search text isn't
        // misinterpreted as a wildcard.
        var escapedQuery = query.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

        return await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && EF.Functions.ILike(u.Name, $"%{escapedQuery}%"))
            .OrderBy(u => u.Name)
            .Take(20)
            .ToListAsync(ct);
    }

    public async Task<List<User>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Users.AsNoTracking().OrderBy(u => u.Name).ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _context.Users.AddAsync(user, ct);
}

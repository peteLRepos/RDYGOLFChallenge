using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GolfClub.Infrastructure.Persistence.Repositories;

public class CartRepository : ICartRepository
{
    private readonly GolfClubDbContext _context;

    public CartRepository(GolfClubDbContext context)
    {
        _context = context;
    }

    public Task<Cart?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Carts.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<List<Cart>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Carts.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public async Task AddAsync(Cart cart, CancellationToken ct = default) =>
        await _context.Carts.AddAsync(cart, ct);

    public void Remove(Cart cart) => _context.Carts.Remove(cart);
}

using GolfClub.Domain.Entities;

namespace GolfClub.Application.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Cart>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Cart cart, CancellationToken ct = default);
    void Remove(Cart cart);
}

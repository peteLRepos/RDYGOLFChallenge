using GolfClub.Domain.Entities;

namespace GolfClub.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<List<User>> SearchByNameAsync(string query, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}

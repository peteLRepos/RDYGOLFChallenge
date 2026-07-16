using GolfClub.Domain.Entities;

namespace GolfClub.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Same lookup as <see cref="GetByEmailAsync"/>, but tracked — for the one caller
    /// that needs to mutate the result (UserService.ForgotPasswordAsync's password reset) rather
    /// than just read it. GetByEmailAsync stays no-tracking for its read-only callers (login,
    /// email-uniqueness checks).</summary>
    Task<User?> GetTrackedByEmailAsync(string email, CancellationToken ct = default);
    Task<List<User>> SearchByNameAsync(string query, CancellationToken ct = default);
    Task<List<User>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}

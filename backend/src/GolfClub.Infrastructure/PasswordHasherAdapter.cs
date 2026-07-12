using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GolfClub.Infrastructure;

public class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    // PasswordHasher<TUser>'s default implementation doesn't actually use the user instance —
    // it's only there for callers who override hashing behavior per-user. Safe to pass null here.
    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string password, string passwordHash)
    {
        var result = _hasher.VerifyHashedPassword(null!, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}

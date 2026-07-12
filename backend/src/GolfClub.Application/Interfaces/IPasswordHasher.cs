namespace GolfClub.Application.Interfaces;

/// <summary>
/// Abstracts password hashing so Domain/Application never depend on a specific hashing library.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

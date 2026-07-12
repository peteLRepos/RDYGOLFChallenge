using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool IsAdmin { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User()
    {
    }

    /// <param name="passwordHash">
    /// Already-hashed password, computed by the caller via IPasswordHasher — this entity never
    /// hashes/verifies passwords itself, keeping Domain free of any hashing library dependency.
    /// </param>
    /// <param name="now">
    /// Supplied by the caller (via IDateTimeProvider), not read from the system clock here — same
    /// testability rationale as Booking.
    /// </param>
    public User(string name, string email, string passwordHash, DateTime now, bool isAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password is required.");

        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        IsAdmin = isAdmin;
        IsActive = true;
        CreatedAt = now;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void PromoteToAdmin() => IsAdmin = true;

    public void DemoteFromAdmin() => IsAdmin = false;
}

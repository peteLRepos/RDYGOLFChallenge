using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Entities;

public class User
{
    // Single source of truth for these limits — UserConfiguration's HasMaxLength references the
    // same constants, so the DB column size and this validation can never drift apart.
    public const int MaxNameLength = 200;
    public const int MaxEmailLength = 320;

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
        if (name.Length > MaxNameLength)
            throw new DomainException($"Name must be at most {MaxNameLength} characters.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password is required.");

        // Normalized so email comparisons (uniqueness, login lookup) are case-insensitive — no
        // mainstream email provider treats casing as significant, and callers shouldn't have to
        // remember to normalize before every lookup. Length is checked after normalizing since
        // that's what actually gets stored (trimming can only shorten it).
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (normalizedEmail.Length > MaxEmailLength)
            throw new DomainException($"Email must be at most {MaxEmailLength} characters.");

        Id = Guid.NewGuid();
        Name = name;
        Email = normalizedEmail;
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

using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Entities;

public class User
{
    // Single source of truth for these limits — UserConfiguration's HasMaxLength references the
    // same constants, so the DB column size and this validation can never drift apart.
    public const int MaxNameLength = 200;
    public const int MaxEmailLength = 320;

    // Golf handicap range: -10 is an elite "plus" handicap, 56 is the default assigned when a
    // new member doesn't provide one (worst/beginner end, matches "not established yet").
    public const int MinHandicap = -10;
    public const int MaxHandicap = 56;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool IsAdmin { get; private set; }
    public bool IsActive { get; private set; }
    public int Handicap { get; private set; }
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
    public User(string name, string email, string passwordHash, DateTime now, int? handicap = null, bool isAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");
        if (name.Length > MaxNameLength)
            throw new DomainException($"Name must be at most {MaxNameLength} characters.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password is required.");
        if (handicap is < MinHandicap or > MaxHandicap)
            throw new DomainException($"Handicap must be between {MinHandicap} and {MaxHandicap}.");

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
        // No handicap established yet defaults to the worst/beginner end of the range, not the
        // best — an unknown golfer shouldn't be treated as if they were scratch.
        Handicap = handicap ?? MaxHandicap;
        CreatedAt = now;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void PromoteToAdmin() => IsAdmin = true;

    public void DemoteFromAdmin() => IsAdmin = false;

    /// <param name="newPasswordHash">Already-hashed, same contract as the constructor's passwordHash.</param>
    public void ResetPassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password is required.");

        PasswordHash = newPasswordHash;
    }
}

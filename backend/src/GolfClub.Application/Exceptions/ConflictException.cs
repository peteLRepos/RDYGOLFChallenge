namespace GolfClub.Application.Exceptions;

/// <summary>
/// Thrown when a save fails a database-level uniqueness constraint — the backstop for
/// check-then-act race conditions (e.g. two concurrent registrations with the same email).
/// Infrastructure translates the underlying provider exception into this generic type so
/// Application/Domain never need to know about EF Core or Npgsql.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

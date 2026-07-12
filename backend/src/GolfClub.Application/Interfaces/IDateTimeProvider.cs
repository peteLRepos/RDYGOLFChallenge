namespace GolfClub.Application.Interfaces;

/// <summary>
/// Abstracts "now" so domain logic can be tested with fixed points in time instead of the real clock.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Current club-local time, as a naive (Kind=Unspecified) value — consistent with how
    /// Booking.Start/End/CreatedAt are stored (see README's timezone assumptions).
    /// </summary>
    DateTime Now { get; }
}

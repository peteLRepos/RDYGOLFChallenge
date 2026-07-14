using GolfClub.Application.DTOs;
using GolfClub.Domain.Entities;

namespace GolfClub.Application.Services;

public interface IWaitlistService
{
    Task<WaitlistEntryDto> JoinAsync(Guid resourceId, DateTime slotStart, Guid userId, CancellationToken ct = default);
    Task LeaveAsync(Guid entryId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
    Task<List<WaitlistEntryDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Offers a freed seat to whoever's queued for this resource+slot, oldest first, up to however
    /// many seats are actually open — called by BookingService after a cancellation or player
    /// removal. Deliberately does not call SaveChangesAsync itself: this runs as one step inside the
    /// caller's own unit of work, which persists the booking change and the fulfillment together in
    /// a single transaction.
    /// </summary>
    /// <param name="currentBooking">
    /// The still-live booking occupying this slot, if any — null when the whole booking was just
    /// cancelled. Must be the caller's own tracked instance, not a fresh query: a separately
    /// (no-tracking) re-fetched copy wouldn't see the caller's not-yet-saved change, and any AddPlayer
    /// made on it here would be silently lost since it's a different object than the one the
    /// DbContext actually persists.
    /// </param>
    Task FulfillAsync(Resource resource, DateTime slotStart, Booking? currentBooking, CancellationToken ct = default);
}

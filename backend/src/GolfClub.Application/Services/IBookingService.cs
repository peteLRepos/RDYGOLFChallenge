using GolfClub.Application.DTOs;
using GolfClub.Domain.Enums;

namespace GolfClub.Application.Services;

public interface IBookingService
{
    Task<List<TimeSlotDto>> GetAvailabilityAsync(Guid resourceId, DateOnly date, CancellationToken ct = default);
    Task<List<BookingDto>> GetAllAsync(CancellationToken ct = default);
    /// <summary>Bookings the user either created or joined as a named player.</summary>
    Task<List<BookingDto>> GetMyBookingsAsync(Guid userId, CancellationToken ct = default);
    Task<BookingDto> GetByIdAsync(Guid id, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
    Task<BookingDto> CreateAsync(CreateBookingRequest request, Guid bookerId, CancellationToken ct = default);

    /// <summary>
    /// Admin-only — the booker is whichever user the admin put in the first slot, not the caller,
    /// so unlike <see cref="CreateAsync"/> there's no "caller must be the first player" check.
    /// </summary>
    Task<BookingDto> AdminCreateAsync(CreateBookingRequest request, CancellationToken ct = default);
    Task CancelAsync(Guid bookingId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
    Task CheckInAsync(Guid bookingId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);

    /// <summary>
    /// Admin-only — no <c>requestingUserId</c>/<c>isAdmin</c> params because there's no owner path
    /// for this operation to distinguish (unlike Cancel/CheckIn, a regular user never marks their
    /// own booking paid). Access is gated entirely by the controller's [Authorize(Roles="Admin")].
    /// </summary>
    Task MarkPaidAsync(Guid bookingId, CancellationToken ct = default);

    /// <summary>
    /// Admin-only, same reasoning as <see cref="MarkPaidAsync"/> — moving a booking is never
    /// exposed on the public booking flow, only via the admin controller.
    /// </summary>
    Task<BookingDto> MoveAsync(Guid bookingId, MoveBookingRequest request, CancellationToken ct = default);

    /// <summary>
    /// Adds a named player — <paramref name="targetUserId"/> is who's being added,
    /// <paramref name="requestingUserId"/>/<paramref name="isAdmin"/> is who's asking. Allowed if
    /// the requester is the booker, an admin, or adding themselves (self-join).
    /// </summary>
    Task<BookingDto> AddPlayerAsync(Guid bookingId, Guid targetUserId, PaymentMethod paymentMethod, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);

    /// <summary>
    /// Removes a player who isn't the original booker. Allowed for the player themselves
    /// (self-unbook), for whoever added them (e.g. the booker removing a guest they invited), or
    /// for an admin. See <see cref="Domain.Entities.Booking.RemovePlayer"/> for the exact rule.
    /// </summary>
    Task<BookingDto> RemovePlayerAsync(Guid bookingId, Guid targetUserId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
}

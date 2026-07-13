using GolfClub.Application.DTOs;

namespace GolfClub.Application.Services;

public interface IBookingService
{
    Task<List<TimeSlotDto>> GetAvailabilityAsync(Guid resourceId, DateOnly date, CancellationToken ct = default);
    Task<List<BookingDto>> GetAllAsync(CancellationToken ct = default);
    Task<List<BookingDto>> GetMyBookingsAsync(Guid bookerId, CancellationToken ct = default);
    Task<BookingDto> GetByIdAsync(Guid id, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
    Task<BookingDto> CreateAsync(CreateBookingRequest request, Guid bookerId, CancellationToken ct = default);
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
    Task<BookingDto> AddPlayerAsync(Guid bookingId, Guid targetUserId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
}

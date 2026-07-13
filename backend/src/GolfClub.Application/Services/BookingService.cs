using GolfClub.Application.DTOs;
using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookings;
    private readonly IResourceRepository _resources;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BookingService(
        IBookingRepository bookings,
        IResourceRepository resources,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _bookings = bookings;
        _resources = resources;
        _users = users;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<List<TimeSlotDto>> GetAvailabilityAsync(Guid resourceId, DateOnly date, CancellationToken ct = default)
    {
        var resource = await _resources.GetByIdAsync(resourceId, ct)
            ?? throw new NotFoundException($"Resource '{resourceId}' was not found.");

        var existingBookings = await _bookings.GetByResourceAndDateAsync(resourceId, date, ct);

        var slots = new List<TimeSlotDto>();
        var slotStart = date.ToDateTime(resource.OpeningTime);
        var dayEnd = date.ToDateTime(resource.ClosingTime);
        var duration = TimeSpan.FromMinutes(resource.SlotDurationMinutes);

        while (slotStart + duration <= dayEnd)
        {
            var slotEnd = slotStart + duration;
            var isAvailable = existingBookings.All(b => !b.OverlapsWith(slotStart, slotEnd));
            slots.Add(new TimeSlotDto(slotStart, slotEnd, isAvailable));
            slotStart = slotEnd;
        }

        return slots;
    }

    public async Task<List<BookingDto>> GetAllAsync(CancellationToken ct = default)
    {
        var bookings = await _bookings.GetAllAsync(ct);
        return bookings.Select(ToDto).ToList();
    }

    public async Task<List<BookingDto>> GetMyBookingsAsync(Guid bookerId, CancellationToken ct = default)
    {
        var bookings = await _bookings.GetByBookerAsync(bookerId, ct);
        return bookings.Select(ToDto).ToList();
    }

    public async Task<BookingDto> GetByIdAsync(Guid id, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Booking '{id}' was not found.");

        EnsureOwnerOrAdmin(booking, requestingUserId, isAdmin);
        return ToDto(booking);
    }

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, Guid bookerId, CancellationToken ct = default)
    {
        var resource = await ValidateSlotAsync(request.ResourceId, request.Start, request.End, excludeBookingId: null, ct);
        var booker = await _users.GetByIdAsync(bookerId, ct)
            ?? throw new NotFoundException($"User '{bookerId}' was not found.");

        var booking = new Booking(
            request.ResourceId,
            bookerId,
            request.Start,
            request.End,
            request.PaymentMethod,
            booker.Handicap,
            resource.PricePerPlayer ?? 0m,
            _dateTimeProvider.Now);
        await _bookings.AddAsync(booking, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Re-fetch rather than mapping `booking` directly: the Resource/Booker/Players navigation
        // properties on the just-created entity were never loaded (no Include on a new entity),
        // so ToDto would see them as null/empty. GetByIdAsync already includes all three.
        return ToDto((await _bookings.GetByIdAsync(booking.Id, ct))!);
    }

    public async Task CancelAsync(Guid bookingId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdAsync(bookingId, ct)
            ?? throw new NotFoundException($"Booking '{bookingId}' was not found.");

        EnsureOwnerOrAdmin(booking, requestingUserId, isAdmin);
        booking.Cancel();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task CheckInAsync(Guid bookingId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdAsync(bookingId, ct)
            ?? throw new NotFoundException($"Booking '{bookingId}' was not found.");

        EnsureOwnerOrAdmin(booking, requestingUserId, isAdmin);
        booking.CheckIn(_dateTimeProvider.Now);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task MarkPaidAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdAsync(bookingId, ct)
            ?? throw new NotFoundException($"Booking '{bookingId}' was not found.");

        booking.MarkPaid();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<BookingDto> MoveAsync(Guid bookingId, MoveBookingRequest request, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdAsync(bookingId, ct)
            ?? throw new NotFoundException($"Booking '{bookingId}' was not found.");

        var resource = await ValidateSlotAsync(request.ResourceId, request.Start, request.End, excludeBookingId: bookingId, ct);
        booking.Reschedule(request.ResourceId, request.Start, request.End, resource.PricePerPlayer ?? 0m, _dateTimeProvider.Now);
        await _unitOfWork.SaveChangesAsync(ct);

        // Re-fetch: Reschedule may have changed ResourceId, but the already-loaded Resource
        // navigation still points at the old resource until reloaded from the database.
        return ToDto((await _bookings.GetByIdAsync(booking.Id, ct))!);
    }

    /// <summary>
    /// Adds a named player to a booking — used both when the booker invites a specific guest and
    /// when another user self-joins. <paramref name="targetUserId"/> is who's being added;
    /// <paramref name="requestingUserId"/>/<paramref name="isAdmin"/> is who's asking.
    /// </summary>
    public async Task<BookingDto> AddPlayerAsync(Guid bookingId, Guid targetUserId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdAsync(bookingId, ct)
            ?? throw new NotFoundException($"Booking '{bookingId}' was not found.");

        // Allowed: the booker or an admin inviting anyone, or any user adding themselves.
        if (!isAdmin && requestingUserId != booking.BookerId && requestingUserId != targetUserId)
            throw new ForbiddenException("Only the booker or an admin can add another player to this booking.");

        var targetUser = await _users.GetByIdAsync(targetUserId, ct)
            ?? throw new NotFoundException($"User '{targetUserId}' was not found.");
        var resource = await _resources.GetByIdAsync(booking.ResourceId, ct)
            ?? throw new NotFoundException($"Resource '{booking.ResourceId}' was not found.");

        booking.AddPlayer(targetUserId, targetUser.Handicap, resource.PricePerPlayer ?? 0m, _dateTimeProvider.Now);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto((await _bookings.GetByIdAsync(booking.Id, ct))!);
    }

    // Returns the validated Resource so callers that need its PricePerPlayer (Create/Move) don't
    // have to re-fetch it — they already both re-fetch the Booking afterward for its DTO anyway
    // (see those methods' comments), so this at least avoids a second, separate Resources lookup.
    private async Task<Resource> ValidateSlotAsync(Guid resourceId, DateTime start, DateTime end, Guid? excludeBookingId, CancellationToken ct)
    {
        var resource = await _resources.GetByIdAsync(resourceId, ct)
            ?? throw new NotFoundException($"Resource '{resourceId}' was not found.");

        if (!resource.IsActive)
            throw new DomainException("This resource is not currently bookable.");

        if (!resource.IsWithinOperatingHours(start, end))
            throw new DomainException("The requested time is outside this resource's operating hours.");

        var hasOverlap = await _bookings.HasOverlapAsync(resourceId, start, end, excludeBookingId, ct);
        if (hasOverlap)
            throw new DomainException("This time slot is already booked.");

        return resource;
    }

    private static void EnsureOwnerOrAdmin(Booking booking, Guid requestingUserId, bool isAdmin)
    {
        if (!isAdmin && booking.BookerId != requestingUserId)
            throw new ForbiddenException("You do not have permission to access this booking.");
    }

    private static BookingDto ToDto(Booking booking) => new(
        booking.Id,
        booking.ResourceId,
        booking.Resource?.Name ?? string.Empty,
        booking.BookerId,
        booking.Booker?.Name ?? string.Empty,
        booking.Booker?.Email ?? string.Empty,
        booking.Start,
        booking.End,
        booking.PaymentMethod,
        booking.IsPaid,
        booking.Status,
        booking.PlayerCount,
        booking.CombinedHandicap,
        booking.Players.Select(p => new BookingPlayerDto(p.UserId, p.User?.Name ?? string.Empty, p.Handicap)).ToList(),
        booking.TotalPrice,
        booking.CreatedAt);
}

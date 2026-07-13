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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BookingService(
        IBookingRepository bookings,
        IResourceRepository resources,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _bookings = bookings;
        _resources = resources;
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
        await ValidateSlotAsync(request.ResourceId, request.Start, request.End, excludeBookingId: null, ct);

        var booking = new Booking(
            request.ResourceId,
            bookerId,
            request.Start,
            request.End,
            request.PaymentMethod,
            _dateTimeProvider.Now);
        await _bookings.AddAsync(booking, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Re-fetch rather than mapping `booking` directly: the Resource/Booker navigation
        // properties on the just-created entity were never loaded (no Include on a new entity),
        // so ToDto would see them as null. GetByIdAsync already includes both.
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

        await ValidateSlotAsync(request.ResourceId, request.Start, request.End, excludeBookingId: bookingId, ct);
        booking.Reschedule(request.ResourceId, request.Start, request.End, _dateTimeProvider.Now);
        await _unitOfWork.SaveChangesAsync(ct);

        // Re-fetch: Reschedule may have changed ResourceId, but the already-loaded Resource
        // navigation still points at the old resource until reloaded from the database.
        return ToDto((await _bookings.GetByIdAsync(booking.Id, ct))!);
    }

    // Callers (CreateAsync/MoveAsync) re-fetch the booking afterward for its DTO, which re-reads
    // this same Resource row via the join on Booking.Resource — a second round-trip we accept for
    // low request volume rather than threading the Resource instance back out of validation and
    // building the DTO by hand (Booker still needs a separate fetch either way, so the win is small).
    private async Task ValidateSlotAsync(Guid resourceId, DateTime start, DateTime end, Guid? excludeBookingId, CancellationToken ct)
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
        booking.CreatedAt);
}

using GolfClub.Application.DTOs;
using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Enums;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookings;
    private readonly IResourceRepository _resources;
    private readonly IUserRepository _users;
    private readonly ICartService _cartService;
    private readonly IWaitlistService _waitlistService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BookingService(
        IBookingRepository bookings,
        IResourceRepository resources,
        IUserRepository users,
        ICartService cartService,
        IWaitlistService waitlistService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _bookings = bookings;
        _resources = resources;
        _users = users;
        _cartService = cartService;
        _waitlistService = waitlistService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<List<TimeSlotDto>> GetAvailabilityAsync(Guid resourceId, DateOnly date, CancellationToken ct = default)
    {
        var resource = await _resources.GetByIdAsync(resourceId, ct)
            ?? throw new NotFoundException($"Resource '{resourceId}' was not found.");

        var blockingResourceIds = await GetBlockingResourceIdsAsync(resource, ct);
        var existingBookings = await _bookings.GetByResourceAndDateAsync(blockingResourceIds, date, ct);

        var slots = new List<TimeSlotDto>();
        var slotStart = date.ToDateTime(resource.OpeningTime);
        var dayEnd = date.ToDateTime(resource.ClosingTime);
        var duration = TimeSpan.FromMinutes(resource.SlotDurationMinutes);

        while (slotStart + duration <= dayEnd)
        {
            var slotEnd = slotStart + duration;
            var overlapping = existingBookings.FirstOrDefault(b => b.OverlapsWith(slotStart, slotEnd));

            // A block from a *linked* resource (e.g. a lesson holding this hour of the 6-Hole
            // Course) is unavailable but not joinable — no player count/handicap to show, and no
            // bookingId a client could use to try joining a booking that isn't even on this resource.
            var isSameResourceBooking = overlapping is not null && overlapping.ResourceId == resource.Id;

            slots.Add(overlapping is null
                ? new TimeSlotDto(slotStart, slotEnd, IsAvailable: true, BookingId: null, PlayerCount: null, CombinedHandicap: null)
                : isSameResourceBooking
                    ? new TimeSlotDto(slotStart, slotEnd, IsAvailable: false, overlapping.Id, overlapping.PlayerCount, overlapping.CombinedHandicap)
                    : new TimeSlotDto(slotStart, slotEnd, IsAvailable: false, BookingId: null, PlayerCount: null, CombinedHandicap: null));
            slotStart = slotEnd;
        }

        return slots;
    }

    public async Task<List<BookingDto>> GetAllAsync(CancellationToken ct = default)
    {
        var bookings = await _bookings.GetAllAsync(ct);
        return bookings.Select(ToDto).ToList();
    }

    public async Task<List<BookingDto>> GetMyBookingsAsync(Guid userId, CancellationToken ct = default)
    {
        var bookings = await _bookings.GetForUserAsync(userId, ct);
        return bookings.Select(ToDto).ToList();
    }

    public async Task<BookingDto> GetByIdAsync(Guid id, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Booking '{id}' was not found.");

        EnsureCanView(booking, requestingUserId, isAdmin);
        return ToDto(booking);
    }

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, Guid bookerId, CancellationToken ct = default)
    {
        if (request.Players.Count == 0)
            throw new DomainException("A booking needs at least one player.");
        if (request.Players[0].UserId != bookerId)
            throw new DomainException("The requesting user must be the booking's first player.");

        return await CreateCoreAsync(request, bookerId, ct);
    }

    /// <summary>
    /// Admin-only equivalent of <see cref="CreateAsync"/> — the booker is whichever user the admin
    /// put in the first slot, not the admin's own account, so it skips the "caller must be the
    /// first player" check that the public endpoint enforces.
    /// </summary>
    public async Task<BookingDto> AdminCreateAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        if (request.Players.Count == 0)
            throw new DomainException("A booking needs at least one player.");

        return await CreateCoreAsync(request, request.Players[0].UserId, ct);
    }

    private async Task<BookingDto> CreateCoreAsync(CreateBookingRequest request, Guid bookerId, CancellationToken ct)
    {
        var resource = await ValidateSlotAsync(request.ResourceId, request.Start, request.End, excludeBookingId: null, ct);
        var booker = await _users.GetByIdAsync(bookerId, ct)
            ?? throw new NotFoundException($"User '{bookerId}' was not found.");

        var booking = new Booking(
            request.ResourceId,
            bookerId,
            request.Start,
            request.End,
            request.Players[0].PaymentMethod,
            booker.Handicap,
            resource.PricePerPlayer ?? 0m,
            _dateTimeProvider.Now);

        // Any further players in the request were selected by the booker in the same dialog, so
        // they're all "added by" the booker — a later self-join uses AddPlayerAsync directly instead.
        foreach (var extraPlayer in request.Players.Skip(1))
        {
            var user = await _users.GetByIdAsync(extraPlayer.UserId, ct)
                ?? throw new NotFoundException($"User '{extraPlayer.UserId}' was not found.");
            booking.AddPlayer(extraPlayer.UserId, user.Handicap, extraPlayer.PaymentMethod, addedByUserId: bookerId, resource.PricePerPlayer ?? 0m, _dateTimeProvider.Now);
        }

        if (request.WantsCart)
        {
            if (resource.Type == ResourceType.Simulator)
                throw new DomainException("Golf carts aren't available for simulator bookings.");

            var cartId = await _cartService.FindAvailableCartIdAsync(booking.Start, ct);
            booking.AddCart(cartId, resource.PricePerPlayer ?? 0m);
        }

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
        var resource = await _resources.GetByIdAsync(booking.ResourceId, ct)
            ?? throw new NotFoundException($"Resource '{booking.ResourceId}' was not found.");

        booking.Cancel();
        // Only the booking's own slot start is re-offered to the queue — a cancelled multi-hour
        // simulator booking frees every hour it spanned, but re-checking just the first hour keeps
        // this from having to walk the whole range (a documented simplification, see README).
        // currentBooking: null — the just-cancelled booking no longer occupies the slot.
        await _waitlistService.FulfillAsync(resource, booking.Start, currentBooking: null, ct);
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
    public async Task<BookingDto> AddPlayerAsync(Guid bookingId, Guid targetUserId, PaymentMethod paymentMethod, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
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

        // The requester is who's actually adding the player: the booker/admin when inviting a
        // guest, or the target themselves when self-joining — matches Booking.RemovePlayer's rule.
        booking.AddPlayer(targetUserId, targetUser.Handicap, paymentMethod, addedByUserId: requestingUserId, resource.PricePerPlayer ?? 0m, _dateTimeProvider.Now);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto((await _bookings.GetByIdAsync(booking.Id, ct))!);
    }

    public async Task<BookingDto> RemovePlayerAsync(Guid bookingId, Guid targetUserId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdAsync(bookingId, ct)
            ?? throw new NotFoundException($"Booking '{bookingId}' was not found.");

        // Allowed: an admin, the player removing themselves, or whoever added that player (e.g.
        // the booker removing a guest they invited) — matches AddPlayerAsync's permission shape
        // (checked here, not in Domain, so both endpoints fail the same way: 403, not 400).
        var player = booking.Players.FirstOrDefault(p => p.UserId == targetUserId)
            ?? throw new NotFoundException($"User '{targetUserId}' is not in this booking.");
        var isSelfRemoval = requestingUserId == targetUserId;
        var isRemoverWhoAddedThem = requestingUserId == player.AddedByUserId;
        if (!isAdmin && !isSelfRemoval && !isRemoverWhoAddedThem)
            throw new ForbiddenException("You can only remove a player you added, or remove yourself.");

        var resource = await _resources.GetByIdAsync(booking.ResourceId, ct)
            ?? throw new NotFoundException($"Resource '{booking.ResourceId}' was not found.");

        booking.RemovePlayer(targetUserId, resource.PricePerPlayer ?? 0m, _dateTimeProvider.Now);
        // currentBooking: booking — still live, just with one fewer player, so the freed seat is
        // offered on this same tracked instance.
        await _waitlistService.FulfillAsync(resource, booking.Start, currentBooking: booking, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto((await _bookings.GetByIdAsync(booking.Id, ct))!);
    }

    /// <summary>Attaches a cart to an existing pending booking — booker or admin only, same as Cancel/CheckIn.</summary>
    public async Task<BookingDto> AddCartAsync(Guid bookingId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdAsync(bookingId, ct)
            ?? throw new NotFoundException($"Booking '{bookingId}' was not found.");
        EnsureOwnerOrAdmin(booking, requestingUserId, isAdmin);

        var resource = await _resources.GetByIdAsync(booking.ResourceId, ct)
            ?? throw new NotFoundException($"Resource '{booking.ResourceId}' was not found.");
        if (resource.Type == ResourceType.Simulator)
            throw new DomainException("Golf carts aren't available for simulator bookings.");

        var cartId = await _cartService.FindAvailableCartIdAsync(booking.Start, ct);
        booking.AddCart(cartId, resource.PricePerPlayer ?? 0m);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto((await _bookings.GetByIdAsync(booking.Id, ct))!);
    }

    /// <summary>Detaches a booking's cart — booker or admin only, same as Cancel/CheckIn.</summary>
    public async Task<BookingDto> RemoveCartAsync(Guid bookingId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdAsync(bookingId, ct)
            ?? throw new NotFoundException($"Booking '{bookingId}' was not found.");
        EnsureOwnerOrAdmin(booking, requestingUserId, isAdmin);

        var resource = await _resources.GetByIdAsync(booking.ResourceId, ct)
            ?? throw new NotFoundException($"Resource '{booking.ResourceId}' was not found.");

        booking.RemoveCart(resource.PricePerPlayer ?? 0m);
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

        resource.ValidateBookingDuration(start, end);

        var blockingResourceIds = await GetBlockingResourceIdsAsync(resource, ct);
        var hasOverlap = await _bookings.HasOverlapAsync(blockingResourceIds, start, end, excludeBookingId, ct);
        if (hasOverlap)
            throw new DomainException("This time slot is already booked.");

        return resource;
    }

    /// <summary>
    /// A resource, its linked resource (if any), and anything that links back to it — e.g. for the
    /// 6-Hole Course this returns [6-Hole Course, Lesson with Pro], and for the Lesson resource it
    /// returns [Lesson with Pro, 6-Hole Course]. Used so booking either one blocks (and is blocked
    /// by) the other for the same window, computed live rather than via a stored flag — same
    /// philosophy as cart availability.
    /// </summary>
    private async Task<List<Guid>> GetBlockingResourceIdsAsync(Resource resource, CancellationToken ct)
    {
        var ids = new List<Guid> { resource.Id };
        if (resource.LinkedResourceId.HasValue)
            ids.Add(resource.LinkedResourceId.Value);

        var allResources = await _resources.GetAllAsync(includeInactive: true, ct);
        ids.AddRange(allResources.Where(r => r.LinkedResourceId == resource.Id).Select(r => r.Id));

        return ids.Distinct().ToList();
    }

    private static void EnsureOwnerOrAdmin(Booking booking, Guid requestingUserId, bool isAdmin)
    {
        if (!isAdmin && booking.BookerId != requestingUserId)
            throw new ForbiddenException("You do not have permission to access this booking.");
    }

    // Viewing is broader than owning: a booking's other named players (invited or self-joined)
    // can see it too, not just the original booker — unlike Cancel/CheckIn, which stay booker-only.
    private static void EnsureCanView(Booking booking, Guid requestingUserId, bool isAdmin)
    {
        if (!isAdmin && booking.BookerId != requestingUserId && booking.Players.All(p => p.UserId != requestingUserId))
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
        booking.IsPaid,
        booking.Status,
        booking.PlayerCount,
        booking.CombinedHandicap,
        booking.Players.Select(p => new BookingPlayerDto(p.UserId, p.User?.Name ?? string.Empty, p.Handicap, p.PaymentMethod, p.AddedByUserId)).ToList(),
        booking.TotalPrice,
        booking.CreatedAt,
        booking.CartId,
        booking.Cart?.Name);
}

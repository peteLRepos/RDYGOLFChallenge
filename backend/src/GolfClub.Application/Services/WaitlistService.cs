using GolfClub.Application.DTOs;
using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Enums;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Application.Services;

public class WaitlistService : IWaitlistService
{
    private readonly IWaitlistRepository _waitlist;
    private readonly IResourceRepository _resources;
    private readonly IBookingRepository _bookings;
    private readonly IUserRepository _users;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public WaitlistService(
        IWaitlistRepository waitlist,
        IResourceRepository resources,
        IBookingRepository bookings,
        IUserRepository users,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _waitlist = waitlist;
        _resources = resources;
        _bookings = bookings;
        _users = users;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<WaitlistEntryDto> JoinAsync(Guid resourceId, DateTime slotStart, Guid userId, CancellationToken ct = default)
    {
        var resource = await _resources.GetByIdAsync(resourceId, ct)
            ?? throw new NotFoundException($"Resource '{resourceId}' was not found.");
        if (!resource.IsActive)
            throw new DomainException("This resource is not currently bookable.");

        var slotEnd = slotStart.AddMinutes(resource.SlotDurationMinutes);
        var dayBookings = await _bookings.GetByResourceAndDateAsync(new[] { resourceId }, DateOnly.FromDateTime(slotStart), ct);
        var booking = dayBookings.FirstOrDefault(b => b.OverlapsWith(slotStart, slotEnd));
        if (booking is null || !booking.IsFull)
            throw new DomainException("This slot isn't full — book it directly instead of joining the queue.");

        if (await _waitlist.ExistsAsync(resourceId, slotStart, userId, ct))
            throw new DomainException("You're already on the queue for this slot.");

        var entry = new WaitlistEntry(resourceId, slotStart, userId, _dateTimeProvider.Now);
        await _waitlist.AddAsync(entry, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto((await _waitlist.GetByIdAsync(entry.Id, ct))!);
    }

    public async Task LeaveAsync(Guid entryId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var entry = await _waitlist.GetByIdAsync(entryId, ct)
            ?? throw new NotFoundException($"Waitlist entry '{entryId}' was not found.");

        if (!isAdmin && entry.UserId != requestingUserId)
            throw new ForbiddenException("You do not have permission to remove this waitlist entry.");

        _waitlist.Remove(entry);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<List<WaitlistEntryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var entries = await _waitlist.GetAllAsync(ct);
        return entries.Select(ToDto).ToList();
    }

    public async Task FulfillAsync(Resource resource, DateTime slotStart, Booking? currentBooking, CancellationToken ct = default)
    {
        var entries = await _waitlist.GetByResourceAndSlotAsync(resource.Id, slotStart, ct);
        if (entries.Count == 0)
            return;

        var slotEnd = slotStart.AddMinutes(resource.SlotDurationMinutes);
        var booking = currentBooking;

        foreach (var entry in entries)
        {
            var user = await _users.GetByIdAsync(entry.UserId, ct);
            if (user is null)
            {
                _waitlist.Remove(entry);
                continue;
            }

            try
            {
                if (booking is null)
                {
                    booking = new Booking(
                        resource.Id, entry.UserId, slotStart, slotEnd,
                        PaymentMethod.Cash, user.Handicap, resource.PricePerPlayer ?? 0m, _dateTimeProvider.Now);
                    await _bookings.AddAsync(booking, ct);
                }
                else
                {
                    booking.AddPlayer(entry.UserId, user.Handicap, PaymentMethod.Cash, addedByUserId: entry.UserId, resource.PricePerPlayer ?? 0m, _dateTimeProvider.Now);
                }

                _waitlist.Remove(entry);
            }
            catch (DomainException)
            {
                // Doesn't fit this opening (full, or over the handicap cap) — stays queued for next time.
            }
        }
    }

    private static WaitlistEntryDto ToDto(WaitlistEntry entry) => new(
        entry.Id,
        entry.ResourceId,
        entry.Resource?.Name ?? string.Empty,
        entry.SlotStart,
        entry.UserId,
        entry.User?.Name ?? string.Empty,
        entry.User?.Email ?? string.Empty,
        entry.CreatedAt);
}

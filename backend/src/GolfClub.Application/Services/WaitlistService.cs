using GolfClub.Application.DTOs;
using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Application.Services;

public class WaitlistService : IWaitlistService
{
    private readonly IWaitlistRepository _waitlist;
    private readonly IResourceRepository _resources;
    private readonly IBookingRepository _bookings;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public WaitlistService(
        IWaitlistRepository waitlist,
        IResourceRepository resources,
        IBookingRepository bookings,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _waitlist = waitlist;
        _resources = resources;
        _bookings = bookings;
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
        if (booking is null || booking.PlayerCount < Booking.MaxPlayers)
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

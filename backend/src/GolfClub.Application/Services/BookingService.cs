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

    public BookingService(IBookingRepository bookings, IResourceRepository resources, IUnitOfWork unitOfWork)
    {
        _bookings = bookings;
        _resources = resources;
        _unitOfWork = unitOfWork;
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
        return bookings.Select(b => ToDto(b)).ToList();
    }

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        var resource = await _resources.GetByIdAsync(request.ResourceId, ct)
            ?? throw new NotFoundException($"Resource '{request.ResourceId}' was not found.");

        if (!resource.IsActive)
            throw new DomainException("This resource is not currently bookable.");

        if (!resource.IsWithinOperatingHours(request.Start, request.End))
            throw new DomainException("The requested time is outside this resource's operating hours.");

        var hasOverlap = await _bookings.HasOverlapAsync(request.ResourceId, request.Start, request.End, ct);
        if (hasOverlap)
            throw new DomainException("This time slot is already booked.");

        var booking = new Booking(request.ResourceId, request.Start, request.End, request.CustomerName, request.CustomerEmail);
        await _bookings.AddAsync(booking, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto(booking, resource.Name);
    }

    public async Task CancelAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _bookings.GetByIdAsync(bookingId, ct)
            ?? throw new NotFoundException($"Booking '{bookingId}' was not found.");

        booking.Cancel();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static BookingDto ToDto(Booking booking, string? resourceName = null) => new(
        booking.Id,
        booking.ResourceId,
        resourceName ?? booking.Resource?.Name ?? string.Empty,
        booking.Start,
        booking.End,
        booking.CustomerName,
        booking.CustomerEmail,
        booking.Status,
        booking.CreatedAt);
}

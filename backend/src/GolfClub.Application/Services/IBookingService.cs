using GolfClub.Application.DTOs;

namespace GolfClub.Application.Services;

public interface IBookingService
{
    Task<List<TimeSlotDto>> GetAvailabilityAsync(Guid resourceId, DateOnly date, CancellationToken ct = default);
    Task<List<BookingDto>> GetAllAsync(CancellationToken ct = default);
    Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct = default);
    Task CancelAsync(Guid bookingId, CancellationToken ct = default);
}

using GolfClub.Domain.Entities;

namespace GolfClub.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Booking>> GetByResourceAndDateAsync(Guid resourceId, DateOnly date, CancellationToken ct = default);
    Task<List<Booking>> GetAllAsync(CancellationToken ct = default);
    // "For" rather than "By" — includes bookings the user booked AND bookings they joined as a
    // named player, not just ones they created.
    /// <summary>Bookings the user either created or joined as a named player.</summary>
    Task<List<Booking>> GetForUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> HasOverlapAsync(Guid resourceId, DateTime start, DateTime end, Guid? excludeBookingId = null, CancellationToken ct = default);
    Task AddAsync(Booking booking, CancellationToken ct = default);
}

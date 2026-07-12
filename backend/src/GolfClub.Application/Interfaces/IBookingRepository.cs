using GolfClub.Domain.Entities;

namespace GolfClub.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Booking>> GetByResourceAndDateAsync(Guid resourceId, DateOnly date, CancellationToken ct = default);
    Task<List<Booking>> GetAllAsync(CancellationToken ct = default);
    Task<bool> HasOverlapAsync(Guid resourceId, DateTime start, DateTime end, CancellationToken ct = default);
    Task AddAsync(Booking booking, CancellationToken ct = default);
}

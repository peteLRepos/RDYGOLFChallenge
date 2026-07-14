using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GolfClub.Infrastructure.Persistence.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly GolfClubDbContext _context;

    public BookingRepository(GolfClubDbContext context)
    {
        _context = context;
    }

    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Bookings
            .Include(b => b.Resource)
            .Include(b => b.Booker)
            .Include(b => b.Players).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<List<Booking>> GetByResourceAndDateAsync(Guid resourceId, DateOnly date, CancellationToken ct = default)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = date.ToDateTime(TimeOnly.MaxValue);

        return await _context.Bookings
            .AsNoTracking()
            // Needed so the availability endpoint can report each booked slot's player count and
            // combined handicap — Players is otherwise not loaded, and PlayerCount/CombinedHandicap
            // read off it, not a persisted column.
            .Include(b => b.Players)
            .Where(b => b.ResourceId == resourceId
                        && b.Status != BookingStatus.Cancelled
                        && b.Start < dayEnd
                        && b.End > dayStart)
            .OrderBy(b => b.Start)
            .ToListAsync(ct);
    }

    public async Task<List<Booking>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Resource)
            .Include(b => b.Booker)
            .Include(b => b.Players).ThenInclude(p => p.User)
            .OrderByDescending(b => b.Start)
            .ToListAsync(ct);

    public async Task<List<Booking>> GetForUserAsync(Guid userId, CancellationToken ct = default) =>
        await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Resource)
            .Include(b => b.Booker)
            .Include(b => b.Players).ThenInclude(p => p.User)
            .Where(b => b.BookerId == userId || b.Players.Any(p => p.UserId == userId))
            .OrderByDescending(b => b.Start)
            .ToListAsync(ct);

    public async Task<bool> HasOverlapAsync(Guid resourceId, DateTime start, DateTime end, Guid? excludeBookingId = null, CancellationToken ct = default) =>
        await _context.Bookings.AnyAsync(
            b => b.ResourceId == resourceId
                 && b.Status != BookingStatus.Cancelled
                 && b.Start < end
                 && start < b.End
                 && (excludeBookingId == null || b.Id != excludeBookingId),
            ct);

    public async Task AddAsync(Booking booking, CancellationToken ct = default) =>
        await _context.Bookings.AddAsync(booking, ct);
}

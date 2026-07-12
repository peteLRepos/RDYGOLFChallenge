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
        _context.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<List<Booking>> GetByResourceAndDateAsync(Guid resourceId, DateOnly date, CancellationToken ct = default)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = date.ToDateTime(TimeOnly.MaxValue);

        return await _context.Bookings
            .Where(b => b.ResourceId == resourceId
                        && b.Status == BookingStatus.Confirmed
                        && b.Start < dayEnd
                        && b.End > dayStart)
            .OrderBy(b => b.Start)
            .ToListAsync(ct);
    }

    public async Task<List<Booking>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Bookings
            .Include(b => b.Resource)
            .OrderByDescending(b => b.Start)
            .ToListAsync(ct);

    public async Task<bool> HasOverlapAsync(Guid resourceId, DateTime start, DateTime end, CancellationToken ct = default) =>
        await _context.Bookings.AnyAsync(
            b => b.ResourceId == resourceId
                 && b.Status == BookingStatus.Confirmed
                 && b.Start < end
                 && start < b.End,
            ct);

    public async Task AddAsync(Booking booking, CancellationToken ct = default) =>
        await _context.Bookings.AddAsync(booking, ct);
}

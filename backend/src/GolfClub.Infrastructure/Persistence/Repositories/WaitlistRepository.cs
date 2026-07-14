using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GolfClub.Infrastructure.Persistence.Repositories;

public class WaitlistRepository : IWaitlistRepository
{
    private readonly GolfClubDbContext _context;

    public WaitlistRepository(GolfClubDbContext context)
    {
        _context = context;
    }

    public Task<WaitlistEntry?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.WaitlistEntries
            .Include(w => w.Resource)
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

    // Tracked, not AsNoTracking: BookingService.FulfillWaitlistAsync removes entries straight off
    // this result when a seat frees up, so they need to already be attached to the context.
    public async Task<List<WaitlistEntry>> GetByResourceAndSlotAsync(Guid resourceId, DateTime slotStart, CancellationToken ct = default) =>
        await _context.WaitlistEntries
            .Where(w => w.ResourceId == resourceId && w.SlotStart == slotStart)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<List<WaitlistEntry>> GetAllAsync(CancellationToken ct = default) =>
        await _context.WaitlistEntries
            .AsNoTracking()
            .Include(w => w.Resource)
            .Include(w => w.User)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(Guid resourceId, DateTime slotStart, Guid userId, CancellationToken ct = default) =>
        await _context.WaitlistEntries.AnyAsync(
            w => w.ResourceId == resourceId && w.SlotStart == slotStart && w.UserId == userId, ct);

    public async Task AddAsync(WaitlistEntry entry, CancellationToken ct = default) =>
        await _context.WaitlistEntries.AddAsync(entry, ct);

    public void Remove(WaitlistEntry entry) => _context.WaitlistEntries.Remove(entry);
}

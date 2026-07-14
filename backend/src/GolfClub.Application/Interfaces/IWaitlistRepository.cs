using GolfClub.Domain.Entities;

namespace GolfClub.Application.Interfaces;

public interface IWaitlistRepository
{
    Task<WaitlistEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Active entries for one resource+slot, oldest first — used both to check for a
    /// duplicate join and to work out who's next when a seat frees up.</summary>
    Task<List<WaitlistEntry>> GetByResourceAndSlotAsync(Guid resourceId, DateTime slotStart, CancellationToken ct = default);

    /// <summary>All entries across every resource, for the admin queue dashboard.</summary>
    Task<List<WaitlistEntry>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid resourceId, DateTime slotStart, Guid userId, CancellationToken ct = default);
    Task AddAsync(WaitlistEntry entry, CancellationToken ct = default);
    void Remove(WaitlistEntry entry);
}

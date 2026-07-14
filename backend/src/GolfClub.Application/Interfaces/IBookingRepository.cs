using GolfClub.Domain.Entities;

namespace GolfClub.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Bookings on any of <paramref name="resourceIds"/> overlapping the given date — takes
    /// a set, not a single id, so a resource's linked resource (see Resource.LinkedResourceId) can be
    /// included in the same query when rendering the availability grid.</summary>
    Task<List<Booking>> GetByResourceAndDateAsync(IEnumerable<Guid> resourceIds, DateOnly date, CancellationToken ct = default);
    Task<List<Booking>> GetAllAsync(CancellationToken ct = default);
    // "For" rather than "By" — includes bookings the user booked AND bookings they joined as a
    // named player, not just ones they created.
    /// <summary>Bookings the user either created or joined as a named player.</summary>
    Task<List<Booking>> GetForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Whether any non-cancelled booking on any of <paramref name="resourceIds"/> overlaps
    /// [start, end) — a set, not a single id, for the same cross-resource-linking reason as
    /// GetByResourceAndDateAsync.</summary>
    Task<bool> HasOverlapAsync(IEnumerable<Guid> resourceIds, DateTime start, DateTime end, Guid? excludeBookingId = null, CancellationToken ct = default);
    Task AddAsync(Booking booking, CancellationToken ct = default);

    /// <summary>Cart ids currently held by an active (non-cancelled) booking whose 2-hour cart
    /// reservation window overlaps [start, end) — used to work out which carts are free.</summary>
    Task<List<Guid>> GetReservedCartIdsOverlappingAsync(DateTime start, DateTime end, CancellationToken ct = default);

    /// <summary>Whether any booking (regardless of status) still references this cart — see
    /// CartService.DeleteAsync, which refuses to delete a cart that's ever been used.</summary>
    Task<bool> HasCartReferenceAsync(Guid cartId, CancellationToken ct = default);
}

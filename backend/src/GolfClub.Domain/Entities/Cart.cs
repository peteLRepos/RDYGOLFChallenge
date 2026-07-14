using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Entities;

/// <summary>
/// One golf cart in the club's fleet. Unlike Resource, a Cart has no time-sheet of its own — carts
/// are interchangeable pooled inventory, and which specific cart a booking gets is an implementation
/// detail (see BookingService). Availability is computed from active bookings' cart reservations,
/// not stored here, so cancelling a booking frees its cart with no extra bookkeeping.
/// </summary>
public class Cart
{
    public const int MaxNameLength = 100;

    // A cart reservation is a fixed 2-hour block starting at the booking's own start time,
    // regardless of the underlying resource's slot length (see Booking.CartReservationEnd) —
    // matches how long a round actually takes, not how long the tee-time slot itself is.
    public const int ReservationHours = 2;

    // Flat per-booking add-on, not per player (unlike Resource.PricePerPlayer).
    public const decimal FixedPrice = 30m;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private Cart()
    {
    }

    public Cart(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Cart name is required.");
        if (name.Length > MaxNameLength)
            throw new DomainException($"Cart name must be at most {MaxNameLength} characters.");

        Id = Guid.NewGuid();
        Name = name;
        IsActive = true;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}

using GolfClub.Domain.Enums;

namespace GolfClub.Domain.Entities;

/// <summary>
/// A named player on a Booking. Child entity of the Booking aggregate — only ever created via
/// Booking's constructor or Booking.AddPlayer, never independently, so its constructor is internal
/// rather than public (unlike Resource/User, which are aggregate roots in their own right).
/// </summary>
public class BookingPlayer
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User? User { get; private set; }

    // Snapshotted at join time rather than derived live from User.Handicap on every read — same
    // reasoning as Booking.TotalPrice: if a player's handicap changes later, a booking they've
    // already joined shouldn't retroactively change what it was validated against.
    public int Handicap { get; private set; }

    // Each player pays their own way (spec: "if ANY payment method is cash the booking is unpaid").
    public PaymentMethod PaymentMethod { get; private set; }

    // Who invited this player — the booker (invited a guest) or the player themselves (self-join).
    // Drives Booking.RemovePlayer: the booker may remove a player they personally added, but not
    // someone who joined an open slot on their own initiative (only that player can remove themselves).
    public Guid AddedByUserId { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private BookingPlayer()
    {
    }

    internal BookingPlayer(Guid userId, int handicap, PaymentMethod paymentMethod, Guid addedByUserId, DateTime joinedAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Handicap = handicap;
        PaymentMethod = paymentMethod;
        AddedByUserId = addedByUserId;
        JoinedAt = joinedAt;
    }
}

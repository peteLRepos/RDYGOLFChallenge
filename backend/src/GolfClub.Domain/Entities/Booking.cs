using GolfClub.Domain.Enums;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Entities;

public class Booking
{
    // How far ahead of the start time check-in opens.
    public const int CheckInWindowMinutes = 15;

    // A booking can have at most this many named players...
    public const int MaxPlayers = 4;
    // ...and their handicaps can never sum past this, checked on every add, not just at creation.
    public const int MaxCombinedHandicap = 120;

    private readonly List<BookingPlayer> _players = new();

    // Set once by MarkPaid — once an admin has manually settled a booking, later roster changes
    // (AddPlayer/RemovePlayer) must not let RecomputeIsPaid silently flip IsPaid back to false.
    private bool _manuallyMarkedPaid;

    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public Resource? Resource { get; private set; }
    public Guid BookerId { get; private set; }
    public User? Booker { get; private set; }
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    public bool IsPaid { get; private set; }
    public BookingStatus Status { get; private set; }
    public decimal TotalPrice { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<BookingPlayer> Players => _players.AsReadOnly();
    public int PlayerCount => _players.Count;
    public int CombinedHandicap => _players.Sum(p => p.Handicap);

    private Booking()
    {
    }

    /// <param name="now">
    /// Current club-local time, supplied by the caller (via IDateTimeProvider in the application layer)
    /// rather than read from the system clock here — keeps this constructor deterministic and testable
    /// without mocking. Start/End/now are all naive timestamps representing club-local time (see README).
    /// </param>
    /// <param name="bookerHandicap">The booker's current handicap — they're the booking's first player.</param>
    /// <param name="pricePerPlayer">
    /// The resource's current price (0 if unpriced), supplied by the caller — Booking doesn't know
    /// about Resource beyond its id, matching the existing bookerId/resourceId pattern. TotalPrice is
    /// computed and stored here rather than derived live from Resource on every read, so a later price
    /// change doesn't retroactively alter what an existing booking was charged.
    /// </param>
    public Booking(
        Guid resourceId,
        Guid bookerId,
        DateTime start,
        DateTime end,
        PaymentMethod bookerPaymentMethod,
        int bookerHandicap,
        decimal pricePerPlayer,
        DateTime now)
    {
        if (resourceId == Guid.Empty)
            throw new DomainException("Resource is required.");
        if (bookerId == Guid.Empty)
            throw new DomainException("Booker is required.");
        ValidatePricePerPlayer(pricePerPlayer);
        ValidateTimeRange(start, end, now);
        ValidatePaymentMethod(bookerPaymentMethod);

        Id = Guid.NewGuid();
        ResourceId = resourceId;
        BookerId = bookerId;
        Start = start;
        End = end;
        Status = BookingStatus.Pending;
        CreatedAt = now;
        _players.Add(new BookingPlayer(bookerId, bookerHandicap, bookerPaymentMethod, addedByUserId: bookerId, now));
        Reprice(pricePerPlayer);
    }

    public bool OverlapsWith(DateTime otherStart, DateTime otherEnd)
    {
        return Status != BookingStatus.Cancelled && Start < otherEnd && otherStart < End;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new DomainException("Booking is already cancelled.");
        if (IsPaid)
            throw new DomainException("Only unpaid bookings can be cancelled.");

        Status = BookingStatus.Cancelled;
    }

    public void CheckIn(DateTime now)
    {
        EnsurePending("checked in");
        if (now < Start.AddMinutes(-CheckInWindowMinutes))
            throw new DomainException($"Check-in opens {CheckInWindowMinutes} minutes before the booking starts.");
        if (now > End)
            throw new DomainException("This booking has already ended.");

        Status = BookingStatus.Ready;
    }

    public void MarkPaid()
    {
        if (Status == BookingStatus.Cancelled)
            throw new DomainException("Cannot mark a cancelled booking as paid.");
        if (IsPaid)
            throw new DomainException("Booking is already paid.");

        IsPaid = true;
        _manuallyMarkedPaid = true;
    }

    /// <param name="pricePerPlayer">The target resource's current price (0 if unpriced) — see the constructor for why TotalPrice is recomputed and stored rather than derived live.</param>
    public void Reschedule(Guid resourceId, DateTime start, DateTime end, decimal pricePerPlayer, DateTime now)
    {
        EnsurePending("moved");
        ValidatePricePerPlayer(pricePerPlayer);
        ValidateTimeRange(start, end, now);

        ResourceId = resourceId;
        Start = start;
        End = end;
        TotalPrice = pricePerPlayer * PlayerCount;
    }

    /// <summary>
    /// Adds a named player — used both when the booker invites a specific guest and when another
    /// user self-joins; the distinction between those two cases is an authorization decision made
    /// by the caller (see BookingService.AddPlayerAsync), but <paramref name="addedByUserId"/> is
    /// still recorded here since it drives what <see cref="RemovePlayer"/> later allows.
    /// </summary>
    /// <param name="pricePerPlayer">The resource's current price (0 if unpriced) — see the constructor for why TotalPrice is recomputed and stored.</param>
    public void AddPlayer(Guid userId, int handicap, PaymentMethod paymentMethod, Guid addedByUserId, decimal pricePerPlayer, DateTime now)
    {
        EnsurePending("joined");
        ValidatePricePerPlayer(pricePerPlayer);
        ValidatePaymentMethod(paymentMethod);
        if (_players.Any(p => p.UserId == userId))
            throw new DomainException("This user is already in the booking.");
        if (_players.Count >= MaxPlayers)
            throw new DomainException($"A booking can have at most {MaxPlayers} players.");
        if (CombinedHandicap + handicap > MaxCombinedHandicap)
            throw new DomainException($"Adding this player would push the combined handicap over {MaxCombinedHandicap}.");

        _players.Add(new BookingPlayer(userId, handicap, paymentMethod, addedByUserId, now));
        Reprice(pricePerPlayer);
    }

    /// <summary>
    /// Removes a player who isn't the original booker (the booker can never be removed here —
    /// cancel the whole booking instead). Who is allowed to call this for a given player is an
    /// authorization decision made by the caller (see BookingService.RemovePlayerAsync); this
    /// method only enforces the business rule that the booker can't be removed.
    /// </summary>
    /// <param name="pricePerPlayer">The resource's current price (0 if unpriced) — see the constructor for why TotalPrice is recomputed and stored.</param>
    public void RemovePlayer(Guid targetUserId, decimal pricePerPlayer, DateTime now)
    {
        EnsurePending("removed");
        ValidatePricePerPlayer(pricePerPlayer);
        if (targetUserId == BookerId)
            throw new DomainException("The original booker cannot be removed — cancel the booking instead.");

        var player = _players.FirstOrDefault(p => p.UserId == targetUserId)
            ?? throw new DomainException("This user is not in the booking.");

        _players.Remove(player);
        Reprice(pricePerPlayer);
    }

    // Shared by AddPlayer/RemovePlayer: both change the roster, so both must recompute the
    // per-player-driven total and paid status the same way.
    private void Reprice(decimal pricePerPlayer)
    {
        TotalPrice = pricePerPlayer * PlayerCount;
        RecomputeIsPaid();
    }

    // A booking is paid only once every player is — one cash player is enough to leave the whole
    // booking unpaid until an admin settles it (see MarkPaid). Recomputed on every player add/remove
    // rather than left stale from whatever it was at creation time — except once MarkPaid has been
    // called manually, which must stick regardless of later roster changes.
    private void RecomputeIsPaid()
    {
        if (_manuallyMarkedPaid)
            return;

        IsPaid = _players.All(p => p.PaymentMethod == PaymentMethod.Card);
    }

    // Real payment processing is out of scope for this project (see README) — the option is kept
    // in the enum so the UI can list it (greyed out), but the backend never accepts it.
    private static void ValidatePaymentMethod(PaymentMethod paymentMethod)
    {
        if (paymentMethod == PaymentMethod.SerialTicket)
            throw new DomainException("Serial ticket payments are not available yet.");
    }

    private void EnsurePending(string action)
    {
        if (Status != BookingStatus.Pending)
            throw new DomainException($"Only a pending booking can be {action}.");
    }

    private static void ValidateTimeRange(DateTime start, DateTime end, DateTime now)
    {
        if (start >= end)
            throw new DomainException("Booking start must be before its end.");
        if (start < now)
            throw new DomainException("Cannot set a booking start time in the past.");
    }

    // 0 is a valid input here (an unpriced resource) — only reject something that couldn't have
    // come from a real Resource.PricePerPlayer (which is already validated null-or-positive).
    private static void ValidatePricePerPlayer(decimal pricePerPlayer)
    {
        if (pricePerPlayer < 0)
            throw new DomainException("Price per player cannot be negative.");
    }
}

using GolfClub.Domain.Enums;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Entities;

public class Booking
{
    // How far ahead of the start time check-in opens.
    public const int CheckInWindowMinutes = 15;

    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public Resource? Resource { get; private set; }
    public Guid BookerId { get; private set; }
    public User? Booker { get; private set; }
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public bool IsPaid { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Booking()
    {
    }

    /// <param name="now">
    /// Current club-local time, supplied by the caller (via IDateTimeProvider in the application layer)
    /// rather than read from the system clock here — keeps this constructor deterministic and testable
    /// without mocking. Start/End/now are all naive timestamps representing club-local time (see README).
    /// </param>
    public Booking(
        Guid resourceId,
        Guid bookerId,
        DateTime start,
        DateTime end,
        PaymentMethod paymentMethod,
        DateTime now)
    {
        if (bookerId == Guid.Empty)
            throw new DomainException("Booker is required.");
        ValidateTimeRange(start, end, now);
        // Real payment processing is out of scope for this project (see README) — the option is
        // kept in the enum so the UI can list it (greyed out), but the backend never accepts it.
        if (paymentMethod == PaymentMethod.SerialTicket)
            throw new DomainException("Serial ticket payments are not available yet.");

        Id = Guid.NewGuid();
        ResourceId = resourceId;
        BookerId = bookerId;
        Start = start;
        End = end;
        PaymentMethod = paymentMethod;
        // Card payments are recorded as immediately paid (no real processing happens); cash is
        // settled in person later, so it starts unpaid until an admin marks it paid.
        IsPaid = paymentMethod == PaymentMethod.Card;
        Status = BookingStatus.Pending;
        CreatedAt = now;
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
    }

    public void Reschedule(Guid resourceId, DateTime start, DateTime end, DateTime now)
    {
        EnsurePending("moved");
        ValidateTimeRange(start, end, now);

        ResourceId = resourceId;
        Start = start;
        End = end;
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
}

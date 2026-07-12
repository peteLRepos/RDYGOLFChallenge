using GolfClub.Domain.Enums;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Entities;

public class Booking
{
    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public Resource? Resource { get; private set; }
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public string CustomerEmail { get; private set; } = null!;
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
        DateTime start,
        DateTime end,
        string customerName,
        string customerEmail,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Customer name is required.");
        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new DomainException("Customer email is required.");
        if (start >= end)
            throw new DomainException("Booking start must be before its end.");
        if (start < now)
            throw new DomainException("Cannot book a time slot in the past.");

        Id = Guid.NewGuid();
        ResourceId = resourceId;
        Start = start;
        End = end;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        Status = BookingStatus.Confirmed;
        CreatedAt = now;
    }

    public bool OverlapsWith(DateTime otherStart, DateTime otherEnd)
    {
        return Status == BookingStatus.Confirmed && Start < otherEnd && otherStart < End;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new DomainException("Booking is already cancelled.");

        Status = BookingStatus.Cancelled;
    }
}

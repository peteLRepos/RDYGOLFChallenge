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

    public Booking(
        Guid resourceId,
        DateTime start,
        DateTime end,
        string customerName,
        string customerEmail)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Customer name is required.");
        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new DomainException("Customer email is required.");
        if (start >= end)
            throw new DomainException("Booking start must be before its end.");
        // Start/End are naive timestamps representing club-local time (see README), so "now" must be
        // compared on the same basis — DateTime.Now, not UtcNow, which would incorrectly reject valid
        // future bookings in timezones behind UTC. This assumes the server's OS timezone is set to the
        // club's local timezone.
        if (start < DateTime.Now)
            throw new DomainException("Cannot book a time slot in the past.");

        Id = Guid.NewGuid();
        ResourceId = resourceId;
        Start = start;
        End = end;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        Status = BookingStatus.Confirmed;
        // Stored as a naive timestamp alongside Start/End (see README) — strip the Kind marker so
        // EF/Npgsql doesn't reject it as a Utc-kind value going into a "timestamp without time zone" column.
        CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
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

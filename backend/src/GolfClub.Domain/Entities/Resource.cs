using GolfClub.Domain.Enums;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Entities;

public class Resource
{
    // Unlike every other resource type, a Simulator booking isn't locked to exactly one generated
    // grid slot — a player can book a multi-hour session, within these bounds.
    public const int MinSimulatorBookingHours = 1;
    public const int MaxSimulatorBookingHours = 5;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public ResourceType Type { get; private set; }
    public int SlotDurationMinutes { get; private set; }
    public TimeOnly OpeningTime { get; private set; }
    public TimeOnly ClosingTime { get; private set; }

    /// <summary>
    /// Null means "not priced yet" — most resource types don't have a set price. Zero is rejected
    /// (see <see cref="ValidateFields"/>) rather than treated as a valid price, so there's exactly
    /// one way to express "unpriced".
    /// </summary>
    public decimal? PricePerPlayer { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Booking this resource also requires (and blocks) the linked resource for the same window,
    /// and vice versa — e.g. a lesson automatically reserves a full hour of the 6-Hole Course, and
    /// that hour can't be tee-time-booked while the lesson holds it. See BookingService for the
    /// bidirectional overlap check. Set only via seed data for now, not admin-editable.
    /// </summary>
    public Guid? LinkedResourceId { get; private set; }

    private Resource()
    {
    }

    public Resource(
        string name,
        ResourceType type,
        int slotDurationMinutes,
        TimeOnly openingTime,
        TimeOnly closingTime,
        decimal? pricePerPlayer = null,
        Guid? linkedResourceId = null)
    {
        ValidateFields(name, slotDurationMinutes, openingTime, closingTime, pricePerPlayer);

        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        SlotDurationMinutes = slotDurationMinutes;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
        PricePerPlayer = pricePerPlayer;
        LinkedResourceId = linkedResourceId;
        IsActive = true;
    }

    public void Update(string name, int slotDurationMinutes, TimeOnly openingTime, TimeOnly closingTime, decimal? pricePerPlayer)
    {
        ValidateFields(name, slotDurationMinutes, openingTime, closingTime, pricePerPlayer);

        Name = name;
        SlotDurationMinutes = slotDurationMinutes;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
        PricePerPlayer = pricePerPlayer;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public bool IsWithinOperatingHours(DateTime start, DateTime end)
    {
        var startTime = TimeOnly.FromDateTime(start);
        var endTime = TimeOnly.FromDateTime(end);
        return startTime >= OpeningTime && endTime <= ClosingTime;
    }

    /// <summary>
    /// Every other resource type is booked in exactly one of its own generated grid slots, so
    /// there's nothing to validate beyond the overlap/operating-hours checks already run. A
    /// Simulator booking's length is up to the player — this enforces the 1-5 whole-hour bounds.
    /// </summary>
    public void ValidateBookingDuration(DateTime start, DateTime end)
    {
        if (Type != ResourceType.Simulator)
            return;

        var duration = end - start;
        if (duration.Ticks % TimeSpan.TicksPerHour != 0
            || duration.TotalHours < MinSimulatorBookingHours
            || duration.TotalHours > MaxSimulatorBookingHours)
        {
            throw new DomainException(
                $"A simulator booking must be a whole number of hours, between {MinSimulatorBookingHours} and {MaxSimulatorBookingHours}.");
        }
    }

    private static void ValidateFields(
        string name, int slotDurationMinutes, TimeOnly openingTime, TimeOnly closingTime, decimal? pricePerPlayer)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Resource name is required.");
        if (slotDurationMinutes <= 0)
            throw new DomainException("Slot duration must be greater than zero.");
        if (openingTime >= closingTime)
            throw new DomainException("Opening time must be before closing time.");
        if (pricePerPlayer is <= 0)
            throw new DomainException("Price per player must be greater than zero, or omitted if this resource isn't priced.");
    }
}

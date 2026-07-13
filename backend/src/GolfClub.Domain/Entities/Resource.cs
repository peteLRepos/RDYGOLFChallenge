using GolfClub.Domain.Enums;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Entities;

public class Resource
{
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

    private Resource()
    {
    }

    public Resource(
        string name,
        ResourceType type,
        int slotDurationMinutes,
        TimeOnly openingTime,
        TimeOnly closingTime,
        decimal? pricePerPlayer = null)
    {
        ValidateFields(name, slotDurationMinutes, openingTime, closingTime, pricePerPlayer);

        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        SlotDurationMinutes = slotDurationMinutes;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
        PricePerPlayer = pricePerPlayer;
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

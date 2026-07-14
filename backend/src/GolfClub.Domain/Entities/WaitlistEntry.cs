using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Entities;

/// <summary>
/// One user's place in line for a specific resource+slot that's currently full. Rows only ever
/// exist while active — fulfilling or leaving the queue deletes the row rather than flagging a
/// status, same "no dead state to track" philosophy as Cart availability.
/// </summary>
public class WaitlistEntry
{
    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public Resource? Resource { get; private set; }
    public DateTime SlotStart { get; private set; }
    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private WaitlistEntry()
    {
    }

    public WaitlistEntry(Guid resourceId, DateTime slotStart, Guid userId, DateTime now)
    {
        if (resourceId == Guid.Empty)
            throw new DomainException("Resource is required.");
        if (userId == Guid.Empty)
            throw new DomainException("User is required.");

        Id = Guid.NewGuid();
        ResourceId = resourceId;
        SlotStart = slotStart;
        UserId = userId;
        CreatedAt = now;
    }
}

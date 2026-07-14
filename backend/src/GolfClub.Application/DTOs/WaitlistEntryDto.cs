namespace GolfClub.Application.DTOs;

public record WaitlistEntryDto(
    Guid Id,
    Guid ResourceId,
    string ResourceName,
    DateTime SlotStart,
    Guid UserId,
    string UserName,
    string UserEmail,
    DateTime CreatedAt);

public record JoinWaitlistRequest(Guid ResourceId, DateTime SlotStart);

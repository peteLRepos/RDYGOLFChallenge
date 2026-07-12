using GolfClub.Domain.Enums;

namespace GolfClub.Application.DTOs;

public record ResourceDto(
    Guid Id,
    string Name,
    ResourceType Type,
    int SlotDurationMinutes,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    bool IsActive);

public record CreateResourceRequest(
    string Name,
    ResourceType Type,
    int SlotDurationMinutes,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime);

public record UpdateResourceRequest(
    string Name,
    int SlotDurationMinutes,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime);

using GolfClub.Domain.Enums;

namespace GolfClub.Application.DTOs;

public record ResourceDto(
    Guid Id,
    string Name,
    ResourceType Type,
    int SlotDurationMinutes,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    decimal? PricePerPlayer,
    bool IsActive);

public record CreateResourceRequest(
    string Name,
    ResourceType Type,
    int SlotDurationMinutes,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    decimal? PricePerPlayer);

public record UpdateResourceRequest(
    string Name,
    int SlotDurationMinutes,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    decimal? PricePerPlayer);

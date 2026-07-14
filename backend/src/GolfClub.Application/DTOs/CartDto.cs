namespace GolfClub.Application.DTOs;

public record CartDto(Guid Id, string Name, bool IsActive);

public record CreateCartRequest(string Name);

public record CartAvailabilityDto(bool IsAvailable);

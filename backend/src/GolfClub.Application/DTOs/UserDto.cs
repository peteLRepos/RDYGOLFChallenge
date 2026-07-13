using GolfClub.Domain.Entities;

namespace GolfClub.Application.DTOs;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    bool IsAdmin,
    bool IsActive,
    int Handicap,
    DateTime CreatedAt)
{
    public static UserDto FromEntity(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.IsAdmin,
        user.IsActive,
        user.Handicap,
        user.CreatedAt);
}

/// <summary>
/// Deliberately minimal — used by the public search endpoint, which must never leak email,
/// IsAdmin, or IsActive for arbitrary users. Full detail is only available via the admin endpoints.
/// </summary>
public record UserSearchResultDto(Guid Id, string Name);

public record RegisterUserRequest(string Name, string Email, string Password, int? Handicap);

public record LoginRequest(string Email, string Password);

public record AuthResponseDto(string Token, UserDto User);

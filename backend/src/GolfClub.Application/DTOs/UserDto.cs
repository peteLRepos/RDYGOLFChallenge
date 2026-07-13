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

/// <summary>
/// No email step (explicitly out of scope, see README) — the caller identifies themselves by
/// email and is shown the new password directly in the response.
/// </summary>
public record ForgotPasswordRequest(string Email);

/// <summary>
/// Always returned with 200, whether or not the email matched an account — an unknown email isn't
/// reported as 404, so simply probing status codes can't be used to enumerate registered members.
/// <paramref name="NewPassword"/> is only populated when an account was actually found.
/// </summary>
public record ForgotPasswordResponseDto(bool AccountFound, string? NewPassword);

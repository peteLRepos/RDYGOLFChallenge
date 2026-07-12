using GolfClub.Application.DTOs;

namespace GolfClub.Application.Services;

public interface IUserService
{
    /// <summary>Registers a new user and immediately logs them in (returns a token).</summary>
    Task<AuthResponseDto> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default);

    /// <summary>Minimal id+name results for the booking-flow "who's booking" search — never full user detail.</summary>
    Task<List<UserSearchResultDto>> SearchAsync(string query, CancellationToken ct = default);
}

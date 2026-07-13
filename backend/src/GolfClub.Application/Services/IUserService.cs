using GolfClub.Application.DTOs;

namespace GolfClub.Application.Services;

public interface IUserService
{
    /// <summary>Registers a new user and immediately logs them in (returns a token).</summary>
    Task<AuthResponseDto> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default);

    /// <summary>Minimal id+name results for the booking-flow "who's booking" search — never full user detail.</summary>
    Task<List<UserSearchResultDto>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Generates a new random password for the account matching the given email, saves its hash,
    /// and returns the plaintext once so the UI can display it — there's no email step in this
    /// project (see README), so this is the only way the user learns their new password.
    /// </summary>
    Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);

    /// <summary>Full user detail — admin-only, never exposed via the public search endpoint.</summary>
    Task<List<UserDto>> GetAllAsync(CancellationToken ct = default);

    Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);

    Task SetAdminAsync(Guid id, bool isAdmin, CancellationToken ct = default);
}

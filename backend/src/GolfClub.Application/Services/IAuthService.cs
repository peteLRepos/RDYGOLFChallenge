using GolfClub.Application.DTOs;

namespace GolfClub.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

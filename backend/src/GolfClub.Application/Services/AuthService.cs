using GolfClub.Application.DTOs;
using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;

namespace GolfClub.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public AuthService(IUserRepository users, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // Must match the normalization User's constructor applies, or a valid login typed with
        // different casing than the stored (registration-time) casing would incorrectly fail.
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(normalizedEmail, ct);

        // Same generic message whether the email doesn't exist, the password is wrong, or the
        // account is deactivated — never reveal which, to avoid leaking account existence.
        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var token = _tokenGenerator.GenerateToken(user);
        return new AuthResponseDto(token, UserDto.FromEntity(user));
    }
}

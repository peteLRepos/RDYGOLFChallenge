using FluentAssertions;
using GolfClub.Application.DTOs;
using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;
using GolfClub.Application.Services;
using GolfClub.Domain.Entities;
using Moq;

namespace GolfClub.Application.Tests.Services;

public class AuthServiceTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 8, 0, 0);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenGenerator> _tokenGenerator = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users.Object, _passwordHasher.Object, _tokenGenerator.Object);
    }

    [Fact]
    public async Task LoginAsync_WhenEmailDoesNotExist_ThrowsUnauthorizedWithGenericMessage()
    {
        _users.Setup(u => u.GetByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var request = new LoginRequest("nobody@example.com", "whatever");

        var act = () => _sut.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsWrong_ThrowsUnauthorizedWithSameGenericMessage()
    {
        var user = new User("Alice Smith", "alice@example.com", "hashed-password", Now);
        _users.Setup(u => u.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("wrong-password", "hashed-password")).Returns(false);
        var request = new LoginRequest("alice@example.com", "wrong-password");

        var act = () => _sut.LoginAsync(request);

        // Same message as the "email doesn't exist" case — proves login doesn't leak which part
        // of the credentials was wrong (avoids user enumeration).
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_WhenAccountIsDeactivated_ThrowsUnauthorized()
    {
        var user = new User("Alice Smith", "alice@example.com", "hashed-password", Now);
        user.Deactivate();
        _users.Setup(u => u.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("correct-password", "hashed-password")).Returns(true);
        var request = new LoginRequest("alice@example.com", "correct-password");

        var act = () => _sut.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUser()
    {
        var user = new User("Alice Smith", "alice@example.com", "hashed-password", Now);
        _users.Setup(u => u.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("correct-password", "hashed-password")).Returns(true);
        _tokenGenerator.Setup(t => t.GenerateToken(user)).Returns("fake-jwt-token");
        var request = new LoginRequest("alice@example.com", "correct-password");

        var result = await _sut.LoginAsync(request);

        result.Token.Should().Be("fake-jwt-token");
        result.User.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task LoginAsync_WithDifferentEmailCasingThanStored_StillSucceeds()
    {
        // The stored email is always lowercase (User normalizes it), so the lookup must normalize
        // whatever casing the caller typed, or a valid login with different casing would 401.
        var user = new User("Alice Smith", "alice@example.com", "hashed-password", Now);
        _users.Setup(u => u.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("correct-password", "hashed-password")).Returns(true);
        _tokenGenerator.Setup(t => t.GenerateToken(user)).Returns("fake-jwt-token");
        var request = new LoginRequest("  ALICE@Example.com  ", "correct-password");

        var result = await _sut.LoginAsync(request);

        result.Token.Should().Be("fake-jwt-token");
    }
}

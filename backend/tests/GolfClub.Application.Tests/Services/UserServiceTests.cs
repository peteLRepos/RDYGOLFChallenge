using FluentAssertions;
using GolfClub.Application.DTOs;
using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;
using GolfClub.Application.Services;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Exceptions;
using Moq;

namespace GolfClub.Application.Tests.Services;

public class UserServiceTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 8, 0, 0);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenGenerator> _tokenGenerator = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _dateTimeProvider.Setup(p => p.Now).Returns(Now);
        _sut = new UserService(
            _users.Object, _unitOfWork.Object, _passwordHasher.Object, _tokenGenerator.Object, _dateTimeProvider.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short1")]
    public async Task RegisterAsync_WithPasswordShorterThanMinimumLength_ThrowsWithoutQueryingRepository(string password)
    {
        var request = new RegisterUserRequest("Alice Smith", "alice@example.com", password);

        var act = () => _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Password*");
        _users.Verify(u => u.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ThrowsDomainException()
    {
        _users.Setup(u => u.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User("Alice Smith", "alice@example.com", "hash", Now));
        var request = new RegisterUserRequest("Alice Smith", "alice@example.com", "Password123");

        var act = () => _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExistsWithDifferentCasing_ThrowsDomainException()
    {
        // The stored/looked-up email is always lowercase (User normalizes it), so the duplicate
        // check must normalize the incoming request the same way before querying.
        _users.Setup(u => u.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User("Alice Smith", "alice@example.com", "hash", Now));
        var request = new RegisterUserRequest("Alice Duplicate", "  Alice@Example.com  ", "Password123");

        var act = () => _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_HashesPasswordSavesUserAndReturnsToken()
    {
        _users.Setup(u => u.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(p => p.Hash("Password123")).Returns("hashed-password");
        _tokenGenerator.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("fake-jwt-token");
        var request = new RegisterUserRequest("Alice Smith", "alice@example.com", "Password123");

        var result = await _sut.RegisterAsync(request);

        result.Token.Should().Be("fake-jwt-token");
        result.User.Name.Should().Be("Alice Smith");
        result.User.Email.Should().Be("alice@example.com");
        result.User.IsAdmin.Should().BeFalse();
        _users.Verify(u => u.AddAsync(
            It.Is<User>(user => user.PasswordHash == "hashed-password"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenSaveRacesAgainstAConcurrentDuplicateRegistration_ThrowsSameDomainExceptionAsSynchronousCheck()
    {
        // Simulates the race: GetByEmailAsync sees no existing user (this request "won" the read),
        // but SaveChangesAsync still fails because a concurrent request committed first — the DB's
        // unique index backstop, translated by Infrastructure into ConflictException. The caller
        // should see the exact same error as the synchronous duplicate-email check, not a 500.
        _users.Setup(u => u.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(p => p.Hash("Password123")).Returns("hashed-password");
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("A record with a conflicting unique value already exists."));
        var request = new RegisterUserRequest("Alice Smith", "alice@example.com", "Password123");

        var act = () => _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already exists*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public async Task SearchAsync_WithQueryShorterThanMinimumLength_ReturnsEmptyWithoutQueryingRepository(string query)
    {
        var results = await _sut.SearchAsync(query);

        results.Should().BeEmpty();
        _users.Verify(u => u.SearchByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsMinimalResults()
    {
        var user = new User("Alice Smith", "alice@example.com", "hash", Now);
        _users.Setup(u => u.SearchByNameAsync("Ali", It.IsAny<CancellationToken>())).ReturnsAsync([user]);

        var results = await _sut.SearchAsync("Ali");

        results.Should().ContainSingle();
        results[0].Id.Should().Be(user.Id);
        results[0].Name.Should().Be("Alice Smith");
    }
}

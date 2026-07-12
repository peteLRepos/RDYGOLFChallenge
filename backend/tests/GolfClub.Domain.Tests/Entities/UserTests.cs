using FluentAssertions;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Tests.Entities;

public class UserTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0);

    [Fact]
    public void Constructor_WithValidData_CreatesActiveNonAdminUser()
    {
        var user = new User("Alice Smith", "alice@example.com", "hashed-password", Now);

        user.IsActive.Should().BeTrue();
        user.IsAdmin.Should().BeFalse();
        user.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Constructor_WithIsAdminTrue_CreatesAdminUser()
    {
        var user = new User("Admin", "admin@testAdmin.com", "hashed-password", Now, isAdmin: true);

        user.IsAdmin.Should().BeTrue();
    }

    [Theory]
    [InlineData("Alice@Example.com", "alice@example.com")]
    [InlineData("  alice@example.com  ", "alice@example.com")]
    [InlineData("ALICE@EXAMPLE.COM", "alice@example.com")]
    public void Constructor_NormalizesEmailCasingAndWhitespace(string input, string expected)
    {
        var user = new User("Alice Smith", input, "hashed-password", Now);

        user.Email.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithMissingName_Throws(string? name)
    {
        var act = () => new User(name!, "alice@example.com", "hashed-password", Now);

        act.Should().Throw<DomainException>().WithMessage("*Name*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithMissingEmail_Throws(string? email)
    {
        var act = () => new User("Alice Smith", email!, "hashed-password", Now);

        act.Should().Throw<DomainException>().WithMessage("*Email*");
    }

    [Fact]
    public void Constructor_WithNameExceedingMaxLength_Throws()
    {
        var tooLongName = new string('A', User.MaxNameLength + 1);

        var act = () => new User(tooLongName, "alice@example.com", "hashed-password", Now);

        act.Should().Throw<DomainException>().WithMessage("*Name*");
    }

    [Fact]
    public void Constructor_WithNameAtMaxLength_DoesNotThrow()
    {
        var maxLengthName = new string('A', User.MaxNameLength);

        var act = () => new User(maxLengthName, "alice@example.com", "hashed-password", Now);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithEmailExceedingMaxLength_Throws()
    {
        var tooLongEmail = new string('a', User.MaxEmailLength - 11) + "@example.com";

        var act = () => new User("Alice Smith", tooLongEmail, "hashed-password", Now);

        act.Should().Throw<DomainException>().WithMessage("*Email*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithMissingPasswordHash_Throws(string? passwordHash)
    {
        var act = () => new User("Alice Smith", "alice@example.com", passwordHash!, Now);

        act.Should().Throw<DomainException>().WithMessage("*Password*");
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var user = new User("Alice Smith", "alice@example.com", "hashed-password", Now);

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var user = new User("Alice Smith", "alice@example.com", "hashed-password", Now);
        user.Deactivate();

        user.Activate();

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PromoteToAdmin_SetsIsAdminTrue()
    {
        var user = new User("Alice Smith", "alice@example.com", "hashed-password", Now);

        user.PromoteToAdmin();

        user.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public void DemoteFromAdmin_SetsIsAdminFalse()
    {
        var user = new User("Admin", "admin@testAdmin.com", "hashed-password", Now, isAdmin: true);

        user.DemoteFromAdmin();

        user.IsAdmin.Should().BeFalse();
    }
}

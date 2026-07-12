using FluentAssertions;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Enums;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Tests.Entities;

public class BookingTests
{
    private static readonly Guid ResourceId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0);

    [Fact]
    public void Constructor_WithValidData_CreatesConfirmedBooking()
    {
        var start = Now.AddHours(1);
        var end = Now.AddHours(2);

        var booking = new Booking(ResourceId, start, end, "Peter", "peter@example.com", Now);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.Start.Should().Be(start);
        booking.End.Should().Be(end);
        booking.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Constructor_WithStartInThePastRelativeToNow_Throws()
    {
        // Start is one hour before "now" — this is the case that used to be checked against
        // DateTime.UtcNow and broke for customers in timezones behind UTC. Passing "now" explicitly
        // means this test is deterministic regardless of when or where it actually runs.
        var start = Now.AddHours(-1);
        var end = Now.AddHours(1);

        var act = () => new Booking(ResourceId, start, end, "Peter", "peter@example.com", Now);

        act.Should().Throw<DomainException>().WithMessage("*in the past*");
    }

    [Fact]
    public void Constructor_WithStartExactlyAtNow_DoesNotThrow()
    {
        var act = () => new Booking(ResourceId, Now, Now.AddMinutes(30), "Peter", "peter@example.com", Now);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithStartAfterEnd_Throws()
    {
        var start = Now.AddHours(2);
        var end = Now.AddHours(1);

        var act = () => new Booking(ResourceId, start, end, "Peter", "peter@example.com", Now);

        act.Should().Throw<DomainException>().WithMessage("*before its end*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithMissingCustomerName_Throws(string? name)
    {
        var act = () => new Booking(ResourceId, Now.AddHours(1), Now.AddHours(2), name!, "peter@example.com", Now);

        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Fact]
    public void OverlapsWith_WhenRangesOverlap_ReturnsTrue()
    {
        var booking = new Booking(ResourceId, Now.AddHours(1), Now.AddHours(2), "Peter", "peter@example.com", Now);

        booking.OverlapsWith(Now.AddHours(1).AddMinutes(30), Now.AddHours(3)).Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_WhenRangesDoNotTouch_ReturnsFalse()
    {
        var booking = new Booking(ResourceId, Now.AddHours(1), Now.AddHours(2), "Peter", "peter@example.com", Now);

        booking.OverlapsWith(Now.AddHours(2), Now.AddHours(3)).Should().BeFalse();
    }

    [Fact]
    public void OverlapsWith_WhenBookingIsCancelled_ReturnsFalse()
    {
        var booking = new Booking(ResourceId, Now.AddHours(1), Now.AddHours(2), "Peter", "peter@example.com", Now);
        booking.Cancel();

        booking.OverlapsWith(Now.AddHours(1), Now.AddHours(2)).Should().BeFalse();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var booking = new Booking(ResourceId, Now.AddHours(1), Now.AddHours(2), "Peter", "peter@example.com", Now);
        booking.Cancel();

        var act = () => booking.Cancel();

        act.Should().Throw<DomainException>().WithMessage("*already cancelled*");
    }
}

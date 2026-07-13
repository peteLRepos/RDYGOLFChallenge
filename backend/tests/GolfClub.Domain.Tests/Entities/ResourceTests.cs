using FluentAssertions;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Enums;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Tests.Entities;

public class ResourceTests
{
    private static Resource CreateResource() =>
        new("Driving Range Bay 1", ResourceType.DrivingRangeBay, 60, new TimeOnly(7, 0), new TimeOnly(21, 0));

    [Fact]
    public void Constructor_WithValidData_CreatesActiveResource()
    {
        var resource = CreateResource();

        resource.IsActive.Should().BeTrue();
        resource.Name.Should().Be("Driving Range Bay 1");
    }

    [Fact]
    public void Constructor_WithOpeningTimeAfterClosingTime_Throws()
    {
        var act = () => new Resource("Bay 1", ResourceType.DrivingRangeBay, 60, new TimeOnly(21, 0), new TimeOnly(7, 0));

        act.Should().Throw<DomainException>().WithMessage("*Opening time*");
    }

    [Fact]
    public void Constructor_WithZeroSlotDuration_Throws()
    {
        var act = () => new Resource("Bay 1", ResourceType.DrivingRangeBay, 0, new TimeOnly(7, 0), new TimeOnly(21, 0));

        act.Should().Throw<DomainException>().WithMessage("*Slot duration*");
    }

    [Fact]
    public void Constructor_WithNoPricePerPlayer_LeavesItNull()
    {
        var resource = CreateResource();

        resource.PricePerPlayer.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithPositivePricePerPlayer_SetsIt()
    {
        var resource = new Resource("6-Hole Course", ResourceType.TeeTime, 10, new TimeOnly(7, 0), new TimeOnly(19, 0), pricePerPlayer: 10m);

        resource.PricePerPlayer.Should().Be(10m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_WithZeroOrNegativePricePerPlayer_Throws(decimal pricePerPlayer)
    {
        var act = () => new Resource("6-Hole Course", ResourceType.TeeTime, 10, new TimeOnly(7, 0), new TimeOnly(19, 0), pricePerPlayer);

        act.Should().Throw<DomainException>().WithMessage("*Price per player*");
    }

    [Fact]
    public void Update_WithPositivePricePerPlayer_SetsIt()
    {
        var resource = CreateResource();

        resource.Update("Bay 1", 60, new TimeOnly(7, 0), new TimeOnly(21, 0), 12.5m);

        resource.PricePerPlayer.Should().Be(12.5m);
    }

    [Fact]
    public void Update_WithNullPricePerPlayer_ClearsIt()
    {
        var resource = new Resource("6-Hole Course", ResourceType.TeeTime, 10, new TimeOnly(7, 0), new TimeOnly(19, 0), pricePerPlayer: 10m);

        resource.Update("6-Hole Course", 10, new TimeOnly(7, 0), new TimeOnly(19, 0), null);

        resource.PricePerPlayer.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Update_WithZeroOrNegativePricePerPlayer_Throws(decimal pricePerPlayer)
    {
        var resource = CreateResource();

        var act = () => resource.Update("Bay 1", 60, new TimeOnly(7, 0), new TimeOnly(21, 0), pricePerPlayer);

        act.Should().Throw<DomainException>().WithMessage("*Price per player*");
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var resource = CreateResource();

        resource.Deactivate();

        resource.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var resource = CreateResource();
        resource.Deactivate();

        resource.Activate();

        resource.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(8, 0, 9, 0, true)]
    [InlineData(6, 0, 8, 0, false)]
    [InlineData(20, 0, 22, 0, false)]
    public void IsWithinOperatingHours_ChecksAgainstOpeningAndClosingTime(
        int startHour, int startMinute, int endHour, int endMinute, bool expected)
    {
        var resource = CreateResource();
        var day = new DateTime(2026, 1, 1);

        var result = resource.IsWithinOperatingHours(
            day.AddHours(startHour).AddMinutes(startMinute),
            day.AddHours(endHour).AddMinutes(endMinute));

        result.Should().Be(expected);
    }
}

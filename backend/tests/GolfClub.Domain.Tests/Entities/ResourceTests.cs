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

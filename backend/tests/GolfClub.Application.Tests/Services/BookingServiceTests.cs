using FluentAssertions;
using GolfClub.Application.DTOs;
using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;
using GolfClub.Application.Services;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Enums;
using GolfClub.Domain.Exceptions;
using Moq;

namespace GolfClub.Application.Tests.Services;

public class BookingServiceTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 8, 0, 0);

    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IResourceRepository> _resources = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly BookingService _sut;

    public BookingServiceTests()
    {
        _dateTimeProvider.Setup(p => p.Now).Returns(Now);
        _sut = new BookingService(_bookings.Object, _resources.Object, _unitOfWork.Object, _dateTimeProvider.Object);
    }

    private static Resource CreateResource(bool isActive = true)
    {
        var resource = new Resource("Simulator Bay", ResourceType.Simulator, 60, new TimeOnly(7, 0), new TimeOnly(21, 0));
        if (!isActive) resource.Deactivate();
        return resource;
    }

    [Fact]
    public async Task CreateAsync_WhenResourceDoesNotExist_ThrowsNotFound()
    {
        var resourceId = Guid.NewGuid();
        _resources.Setup(r => r.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync((Resource?)null);
        var request = new CreateBookingRequest(resourceId, Now.AddHours(1), Now.AddHours(2), "Peter", "peter@example.com");

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WhenResourceIsInactive_ThrowsDomainException()
    {
        var resource = CreateResource(isActive: false);
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        var request = new CreateBookingRequest(Guid.NewGuid(), Now.AddHours(1), Now.AddHours(2), "Peter", "peter@example.com");

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*not currently bookable*");
    }

    [Fact]
    public async Task CreateAsync_WhenOutsideOperatingHours_ThrowsDomainException()
    {
        var resource = CreateResource();
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        var day = Now.Date;
        var request = new CreateBookingRequest(Guid.NewGuid(), day.AddHours(5), day.AddHours(6), "Peter", "peter@example.com");

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*operating hours*");
    }

    [Fact]
    public async Task CreateAsync_WhenSlotOverlapsExistingBooking_ThrowsDomainException()
    {
        var resource = CreateResource();
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookings.Setup(b => b.HasOverlapAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var request = new CreateBookingRequest(Guid.NewGuid(), Now.AddHours(1), Now.AddHours(2), "Peter", "peter@example.com");

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already booked*");
    }

    [Fact]
    public async Task CreateAsync_WhenStartIsBeforeInjectedNow_ThrowsDomainException()
    {
        // Demonstrates the whole point of injecting IDateTimeProvider: this is deterministic
        // regardless of the real wall-clock time when the test suite runs.
        var resource = CreateResource();
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        var request = new CreateBookingRequest(Guid.NewGuid(), Now.AddHours(-1), Now, "Peter", "peter@example.com");

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*in the past*");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_SavesBookingAndReturnsDto()
    {
        var resourceId = Guid.NewGuid();
        var resource = CreateResource();
        _resources.Setup(r => r.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookings.Setup(b => b.HasOverlapAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var request = new CreateBookingRequest(resourceId, Now.AddHours(1), Now.AddHours(2), "Peter", "peter@example.com");

        var result = await _sut.CreateAsync(request);

        result.CustomerName.Should().Be("Peter");
        result.Status.Should().Be(BookingStatus.Confirmed);
        _bookings.Verify(b => b.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_WhenBookingDoesNotExist_ThrowsNotFound()
    {
        _bookings.Setup(b => b.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);

        var act = () => _sut.CancelAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CancelAsync_WhenBookingExists_CancelsAndSaves()
    {
        var booking = new Booking(Guid.NewGuid(), Now.AddHours(1), Now.AddHours(2), "Peter", "peter@example.com", Now);
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        await _sut.CancelAsync(booking.Id);

        booking.Status.Should().Be(BookingStatus.Cancelled);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAvailabilityAsync_GeneratesSlotsForResourceOperatingHours()
    {
        var resourceId = Guid.NewGuid();
        var resource = new Resource("Bay 1", ResourceType.DrivingRangeBay, 60, new TimeOnly(9, 0), new TimeOnly(12, 0));
        _resources.Setup(r => r.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookings.Setup(b => b.GetByResourceAndDateAsync(resourceId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var slots = await _sut.GetAvailabilityAsync(resourceId, DateOnly.FromDateTime(Now));

        slots.Should().HaveCount(3);
        slots.Should().OnlyContain(s => s.IsAvailable);
    }

    [Fact]
    public async Task GetAvailabilityAsync_MarksSlotsOverlappingExistingBookingsAsUnavailable()
    {
        var resourceId = Guid.NewGuid();
        var resource = new Resource("Bay 1", ResourceType.DrivingRangeBay, 60, new TimeOnly(9, 0), new TimeOnly(12, 0));
        _resources.Setup(r => r.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        var day = DateOnly.FromDateTime(Now).ToDateTime(TimeOnly.MinValue);
        var existingBooking = new Booking(resourceId, day.AddHours(10), day.AddHours(11), "Someone", "s@example.com", Now.AddDays(-1));
        _bookings.Setup(b => b.GetByResourceAndDateAsync(resourceId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingBooking]);

        var slots = await _sut.GetAvailabilityAsync(resourceId, DateOnly.FromDateTime(Now));

        slots.Should().HaveCount(3);
        slots.Single(s => s.Start == day.AddHours(10)).IsAvailable.Should().BeFalse();
        slots.Where(s => s.Start != day.AddHours(10)).Should().OnlyContain(s => s.IsAvailable);
    }
}

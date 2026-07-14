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
    private static readonly Guid BookerId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();

    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IResourceRepository> _resources = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICartService> _cartService = new();
    private readonly Mock<IWaitlistRepository> _waitlist = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly BookingService _sut;

    public BookingServiceTests()
    {
        _dateTimeProvider.Setup(p => p.Now).Returns(Now);
        // Default booker for CreateAsync's lookup — individual tests override with a specific
        // user/handicap only when that matters to what they're asserting.
        _users.Setup(u => u.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreateUser());
        // GetBlockingResourceIdsAsync always calls this to resolve linked/linking resources — empty
        // by default since most tests don't involve resource linking (see the lesson/6-hole tests).
        _resources.Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        // CancelAsync/RemovePlayerAsync now look up the booking's resource to try refilling the
        // waitlist — a resource that satisfies that lookup, not tied to any specific test's booking.
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreateResource());
        // No one queued by default — individual waitlist-fulfillment tests override this.
        _waitlist.Setup(w => w.GetByResourceAndSlotAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _sut = new BookingService(_bookings.Object, _resources.Object, _users.Object, _cartService.Object, _waitlist.Object, _unitOfWork.Object, _dateTimeProvider.Object);
    }

    private static Resource CreateResource(bool isActive = true, decimal? pricePerPlayer = null)
    {
        var resource = new Resource("Simulator Bay", ResourceType.Simulator, 60, new TimeOnly(7, 0), new TimeOnly(21, 0), pricePerPlayer);
        if (!isActive) resource.Deactivate();
        return resource;
    }

    private static User CreateUser(int handicap = 20) =>
        new("Player", "player@example.com", "hash", Now, handicap);

    private static Booking CreateBooking(
        Guid? resourceId = null,
        Guid? bookerId = null,
        DateTime? start = null,
        DateTime? end = null,
        PaymentMethod paymentMethod = PaymentMethod.Cash,
        int bookerHandicap = 20,
        decimal pricePerPlayer = 0m) =>
        new(
            resourceId ?? Guid.NewGuid(),
            bookerId ?? BookerId,
            start ?? Now.AddHours(1),
            end ?? Now.AddHours(2),
            paymentMethod,
            bookerHandicap,
            pricePerPlayer,
            Now);

    private static List<PlayerSelectionDto> SoloPlayer(PaymentMethod paymentMethod, Guid? bookerId = null) =>
        [new PlayerSelectionDto(bookerId ?? BookerId, paymentMethod)];

    /// <summary>Stubs the AddAsync/GetByIdAsync round trip CreateAsync does to re-fetch its DTO.</summary>
    private void StubCreateAndReload()
    {
        Booking? saved = null;
        _bookings.Setup(b => b.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => saved = b)
            .Returns(Task.CompletedTask);
        _bookings.Setup(b => b.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved);
    }

    [Fact]
    public async Task CreateAsync_WhenResourceDoesNotExist_ThrowsNotFound()
    {
        var resourceId = Guid.NewGuid();
        _resources.Setup(r => r.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync((Resource?)null);
        var request = new CreateBookingRequest(resourceId, Now.AddHours(1), Now.AddHours(2), SoloPlayer(PaymentMethod.Cash));

        var act = () => _sut.CreateAsync(request, BookerId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WhenResourceIsInactive_ThrowsDomainException()
    {
        var resource = CreateResource(isActive: false);
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        var request = new CreateBookingRequest(Guid.NewGuid(), Now.AddHours(1), Now.AddHours(2), SoloPlayer(PaymentMethod.Cash));

        var act = () => _sut.CreateAsync(request, BookerId);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*not currently bookable*");
    }

    [Fact]
    public async Task CreateAsync_WhenOutsideOperatingHours_ThrowsDomainException()
    {
        var resource = CreateResource();
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        var day = Now.Date;
        var request = new CreateBookingRequest(Guid.NewGuid(), day.AddHours(5), day.AddHours(6), SoloPlayer(PaymentMethod.Cash));

        var act = () => _sut.CreateAsync(request, BookerId);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*operating hours*");
    }

    [Fact]
    public async Task CreateAsync_WhenSlotOverlapsExistingBooking_ThrowsDomainException()
    {
        var resource = CreateResource();
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookings.Setup(b => b.HasOverlapAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var request = new CreateBookingRequest(Guid.NewGuid(), Now.AddHours(1), Now.AddHours(2), SoloPlayer(PaymentMethod.Cash));

        var act = () => _sut.CreateAsync(request, BookerId);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already booked*");
    }

    [Fact]
    public async Task CreateAsync_WhenStartIsBeforeInjectedNow_ThrowsDomainException()
    {
        // Demonstrates the whole point of injecting IDateTimeProvider: this is deterministic
        // regardless of the real wall-clock time when the test suite runs.
        var resource = CreateResource();
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        var request = new CreateBookingRequest(Guid.NewGuid(), Now.AddHours(-1), Now, SoloPlayer(PaymentMethod.Cash));

        var act = () => _sut.CreateAsync(request, BookerId);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*in the past*");
    }

    [Fact]
    public async Task CreateAsync_WithSerialTicket_ThrowsDomainException()
    {
        var resource = CreateResource();
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        var request = new CreateBookingRequest(Guid.NewGuid(), Now.AddHours(1), Now.AddHours(2), SoloPlayer(PaymentMethod.SerialTicket));

        var act = () => _sut.CreateAsync(request, BookerId);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*not available*");
    }

    [Fact]
    public async Task CreateAsync_WithValidCashRequest_SavesUnpaidBookingAndReturnsDto()
    {
        var resourceId = Guid.NewGuid();
        var resource = CreateResource();
        _resources.Setup(r => r.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookings.Setup(b => b.HasOverlapAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        StubCreateAndReload();
        var start = Now.AddHours(1);
        var end = Now.AddHours(2);
        var request = new CreateBookingRequest(resourceId, start, end, SoloPlayer(PaymentMethod.Cash));

        var result = await _sut.CreateAsync(request, BookerId);

        result.ResourceId.Should().Be(resourceId);
        result.Start.Should().Be(start);
        result.End.Should().Be(end);
        result.BookerId.Should().Be(BookerId);
        result.Players.Single().PaymentMethod.Should().Be(PaymentMethod.Cash);
        result.IsPaid.Should().BeFalse();
        result.Status.Should().Be(BookingStatus.Pending);
        _bookings.Verify(b => b.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithCardPayment_ReturnsPaidDto()
    {
        var resourceId = Guid.NewGuid();
        var resource = CreateResource();
        _resources.Setup(r => r.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookings.Setup(b => b.HasOverlapAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        StubCreateAndReload();
        var request = new CreateBookingRequest(resourceId, Now.AddHours(1), Now.AddHours(2), SoloPlayer(PaymentMethod.Card));

        var result = await _sut.CreateAsync(request, BookerId);

        result.IsPaid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithPricedResource_ComputesTotalPriceForTheBooker()
    {
        var resourceId = Guid.NewGuid();
        var resource = CreateResource(pricePerPlayer: 15m);
        _resources.Setup(r => r.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookings.Setup(b => b.HasOverlapAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        StubCreateAndReload();
        var request = new CreateBookingRequest(resourceId, Now.AddHours(1), Now.AddHours(2), SoloPlayer(PaymentMethod.Cash));

        var result = await _sut.CreateAsync(request, BookerId);

        result.PlayerCount.Should().Be(1);
        result.TotalPrice.Should().Be(15m);
    }

    [Fact]
    public async Task CreateAsync_WithUnpricedResource_TotalPriceIsZero()
    {
        var resourceId = Guid.NewGuid();
        var resource = CreateResource(pricePerPlayer: null);
        _resources.Setup(r => r.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookings.Setup(b => b.HasOverlapAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        StubCreateAndReload();
        var request = new CreateBookingRequest(resourceId, Now.AddHours(1), Now.AddHours(2), SoloPlayer(PaymentMethod.Cash));

        var result = await _sut.CreateAsync(request, BookerId);

        result.TotalPrice.Should().Be(0m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookingDoesNotExist_ThrowsNotFound()
    {
        _bookings.Setup(b => b.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);

        var act = () => _sut.GetByIdAsync(Guid.NewGuid(), BookerId, isAdmin: false);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRequestedByOwner_ReturnsDto()
    {
        var booking = CreateBooking();
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var result = await _sut.GetByIdAsync(booking.Id, BookerId, isAdmin: false);

        result.Id.Should().Be(booking.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRequestedByNonOwnerNonAdmin_ThrowsForbidden()
    {
        var booking = CreateBooking();
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var act = () => _sut.GetByIdAsync(booking.Id, OtherUserId, isAdmin: false);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRequestedByAdmin_BypassesOwnership()
    {
        var booking = CreateBooking();
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var result = await _sut.GetByIdAsync(booking.Id, AdminId, isAdmin: true);

        result.Id.Should().Be(booking.Id);
    }

    [Fact]
    public async Task CancelAsync_WhenBookingDoesNotExist_ThrowsNotFound()
    {
        _bookings.Setup(b => b.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);

        var act = () => _sut.CancelAsync(Guid.NewGuid(), BookerId, isAdmin: false);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CancelAsync_WhenOwnerCancelsUnpaidCashBooking_CancelsAndSaves()
    {
        var booking = CreateBooking(paymentMethod: PaymentMethod.Cash);
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        await _sut.CancelAsync(booking.Id, BookerId, isAdmin: false);

        booking.Status.Should().Be(BookingStatus.Cancelled);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_WhenNonOwnerNonAdminCancels_ThrowsForbidden()
    {
        var booking = CreateBooking(paymentMethod: PaymentMethod.Cash);
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var act = () => _sut.CancelAsync(booking.Id, OtherUserId, isAdmin: false);

        await act.Should().ThrowAsync<ForbiddenException>();
        booking.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task CancelAsync_WhenAdminCancelsSomeoneElsesUnpaidBooking_Succeeds()
    {
        var booking = CreateBooking(paymentMethod: PaymentMethod.Cash);
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        await _sut.CancelAsync(booking.Id, AdminId, isAdmin: true);

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_WhenAdminCancelsPaidBooking_Throws()
    {
        // The paid-status gate is enforced by the domain entity itself (Booking.Cancel), so even
        // an admin can't bypass it — matches the spec's "only unpaid bookings can be cancelled".
        var booking = CreateBooking(paymentMethod: PaymentMethod.Card);
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var act = () => _sut.CancelAsync(booking.Id, AdminId, isAdmin: true);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*unpaid*");
    }

    [Fact]
    public async Task CheckInAsync_WhenOwnerCheckInsWithinWindow_SetsReadyAndSaves()
    {
        // Payment method/paid status default to unpaid Cash here deliberately — check-in has no
        // paid-status gate (cash can be settled at the counter), so an unpaid booking checking in
        // successfully is itself proof of that, without needing a separate near-duplicate test.
        var start = Now.AddMinutes(10);
        var booking = CreateBooking(start: start, end: start.AddHours(1));
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        await _sut.CheckInAsync(booking.Id, BookerId, isAdmin: false);

        booking.Status.Should().Be(BookingStatus.Ready);
        booking.IsPaid.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckInAsync_WhenNonOwnerNonAdmin_ThrowsForbidden()
    {
        var start = Now.AddMinutes(10);
        var booking = CreateBooking(start: start, end: start.AddHours(1));
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var act = () => _sut.CheckInAsync(booking.Id, OtherUserId, isAdmin: false);

        await act.Should().ThrowAsync<ForbiddenException>();
        booking.Status.Should().Be(BookingStatus.Pending);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckInAsync_WhenAdminChecksInSomeoneElsesBooking_Succeeds()
    {
        var start = Now.AddMinutes(10);
        var booking = CreateBooking(start: start, end: start.AddHours(1));
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        await _sut.CheckInAsync(booking.Id, AdminId, isAdmin: true);

        booking.Status.Should().Be(BookingStatus.Ready);
    }

    [Fact]
    public async Task CheckInAsync_TooEarly_ThrowsDomainException()
    {
        var start = Now.AddHours(1);
        var booking = CreateBooking(start: start, end: start.AddHours(1));
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var act = () => _sut.CheckInAsync(booking.Id, BookerId, isAdmin: false);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Check-in opens*");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkPaidAsync_WhenBookingDoesNotExist_ThrowsNotFound()
    {
        _bookings.Setup(b => b.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);

        var act = () => _sut.MarkPaidAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MarkPaidAsync_WhenUnpaidCash_MarksPaidAndSaves()
    {
        var booking = CreateBooking(paymentMethod: PaymentMethod.Cash);
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        await _sut.MarkPaidAsync(booking.Id);

        booking.IsPaid.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkPaidAsync_WhenAlreadyPaid_ThrowsDomainException()
    {
        var booking = CreateBooking(paymentMethod: PaymentMethod.Card);
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var act = () => _sut.MarkPaidAsync(booking.Id);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already paid*");
    }

    [Fact]
    public async Task MoveAsync_WhenBookingDoesNotExist_ThrowsNotFound()
    {
        _bookings.Setup(b => b.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);
        var request = new MoveBookingRequest(Guid.NewGuid(), Now.AddHours(3), Now.AddHours(4));

        var act = () => _sut.MoveAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MoveAsync_ToValidSlot_ReschedulesAndReturnsUpdatedDto()
    {
        var newResourceId = Guid.NewGuid();
        var resource = CreateResource();
        var booking = CreateBooking();
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _resources.Setup(r => r.GetByIdAsync(newResourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookings.Setup(b => b.HasOverlapAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(newResourceId)), It.IsAny<DateTime>(), It.IsAny<DateTime>(), booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var newStart = Now.AddHours(3);
        var newEnd = Now.AddHours(4);
        var request = new MoveBookingRequest(newResourceId, newStart, newEnd);

        var result = await _sut.MoveAsync(booking.Id, request);

        booking.ResourceId.Should().Be(newResourceId);
        booking.Start.Should().Be(newStart);
        result.Start.Should().Be(newStart);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveAsync_ToDifferentlyPricedResource_RecomputesTotalPrice()
    {
        var newResourceId = Guid.NewGuid();
        var resource = CreateResource(pricePerPlayer: 22m);
        var booking = CreateBooking(pricePerPlayer: 10m);
        booking.TotalPrice.Should().Be(10m);
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _resources.Setup(r => r.GetByIdAsync(newResourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookings.Setup(b => b.HasOverlapAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(newResourceId)), It.IsAny<DateTime>(), It.IsAny<DateTime>(), booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var request = new MoveBookingRequest(newResourceId, Now.AddHours(3), Now.AddHours(4));

        var result = await _sut.MoveAsync(booking.Id, request);

        result.TotalPrice.Should().Be(22m);
    }

    [Fact]
    public async Task MoveAsync_PassesTheBookingsOwnIdAsExcludeBookingIdToTheOverlapCheck()
    {
        // Unit-level: proves BookingService threads the booking's own id through as
        // excludeBookingId so it doesn't conflict with its own current slot. The actual exclusion
        // filtering happens in BookingRepository.HasOverlapAsync (Infrastructure), which is fully
        // mocked here and has no integration test of its own in this solution.
        var resource = CreateResource();
        var booking = CreateBooking();
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        var request = new MoveBookingRequest(Guid.NewGuid(), booking.Start, booking.End);

        await _sut.MoveAsync(booking.Id, request);

        _bookings.Verify(b => b.HasOverlapAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), booking.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveAsync_WhenTargetSlotOverlapsAnotherBooking_ThrowsDomainException()
    {
        var resource = CreateResource();
        var booking = CreateBooking();
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookings.Setup(b => b.HasOverlapAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var request = new MoveBookingRequest(Guid.NewGuid(), Now.AddHours(3), Now.AddHours(4));

        var act = () => _sut.MoveAsync(booking.Id, request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already booked*");
    }

    [Fact]
    public async Task MoveAsync_WhenBookingIsCheckedIn_ThrowsDomainException()
    {
        var resource = CreateResource();
        var start = Now.AddMinutes(10);
        var booking = CreateBooking(start: start, end: start.AddHours(1));
        booking.CheckIn(start.AddMinutes(-5));
        _bookings.Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _resources.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        var request = new MoveBookingRequest(Guid.NewGuid(), Now.AddHours(3), Now.AddHours(4));

        var act = () => _sut.MoveAsync(booking.Id, request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Only a pending booking*");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryBooking()
    {
        var booking = CreateBooking();
        _bookings.Setup(b => b.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([booking]);

        var result = await _sut.GetAllAsync();

        result.Should().ContainSingle(b => b.Id == booking.Id);
    }

    [Fact]
    public async Task GetMyBookingsAsync_ReturnsOnlyTheCallersBookings()
    {
        var booking = CreateBooking(bookerId: BookerId);
        _bookings.Setup(b => b.GetForUserAsync(BookerId, It.IsAny<CancellationToken>())).ReturnsAsync([booking]);

        var result = await _sut.GetMyBookingsAsync(BookerId);

        result.Should().ContainSingle(b => b.Id == booking.Id);
    }

    [Fact]
    public async Task GetAvailabilityAsync_WhenResourceDoesNotExist_ThrowsNotFound()
    {
        var resourceId = Guid.NewGuid();
        _resources.Setup(r => r.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync((Resource?)null);

        var act = () => _sut.GetAvailabilityAsync(resourceId, DateOnly.FromDateTime(Now));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAvailabilityAsync_GeneratesSlotsForResourceOperatingHours()
    {
        var resource = new Resource("Bay 1", ResourceType.DrivingRangeBay, 60, new TimeOnly(9, 0), new TimeOnly(12, 0));
        _resources.Setup(r => r.GetByIdAsync(resource.Id, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookings.Setup(b => b.GetByResourceAndDateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var slots = await _sut.GetAvailabilityAsync(resource.Id, DateOnly.FromDateTime(Now));

        slots.Should().HaveCount(3);
        slots.Should().OnlyContain(s => s.IsAvailable);
    }

    [Fact]
    public async Task GetAvailabilityAsync_MarksSlotsOverlappingExistingBookingsAsUnavailable()
    {
        var resource = new Resource("Bay 1", ResourceType.DrivingRangeBay, 60, new TimeOnly(9, 0), new TimeOnly(12, 0));
        _resources.Setup(r => r.GetByIdAsync(resource.Id, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        var day = DateOnly.FromDateTime(Now).ToDateTime(TimeOnly.MinValue);
        var existingBooking = CreateBooking(resourceId: resource.Id, start: day.AddHours(10), end: day.AddHours(11));
        _bookings.Setup(b => b.GetByResourceAndDateAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingBooking]);

        var slots = await _sut.GetAvailabilityAsync(resource.Id, DateOnly.FromDateTime(Now));

        slots.Should().HaveCount(3);
        var bookedSlot = slots.Single(s => s.Start == day.AddHours(10));
        bookedSlot.IsAvailable.Should().BeFalse();
        bookedSlot.BookingId.Should().Be(existingBooking.Id);
        slots.Where(s => s.Start != day.AddHours(10)).Should().OnlyContain(s => s.IsAvailable);
    }
}

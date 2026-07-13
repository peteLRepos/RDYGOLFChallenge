using FluentAssertions;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Enums;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Domain.Tests.Entities;

public class BookingTests
{
    private static readonly Guid ResourceId = Guid.NewGuid();
    private static readonly Guid BookerId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0);

    private static Booking CreateBooking(
        DateTime? start = null,
        DateTime? end = null,
        PaymentMethod paymentMethod = PaymentMethod.Cash,
        DateTime? now = null) =>
        new(
            ResourceId,
            BookerId,
            start ?? Now.AddHours(1),
            end ?? Now.AddHours(2),
            paymentMethod,
            now ?? Now);

    [Fact]
    public void Constructor_WithValidCashPayment_CreatesPendingUnpaidBooking()
    {
        var start = Now.AddHours(1);
        var end = Now.AddHours(2);

        var booking = CreateBooking(start, end, PaymentMethod.Cash);

        booking.Status.Should().Be(BookingStatus.Pending);
        booking.PaymentMethod.Should().Be(PaymentMethod.Cash);
        booking.IsPaid.Should().BeFalse();
        booking.BookerId.Should().Be(BookerId);
        booking.Start.Should().Be(start);
        booking.End.Should().Be(end);
        booking.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Constructor_WithCardPayment_IsPaidImmediately()
    {
        var booking = CreateBooking(paymentMethod: PaymentMethod.Card);

        booking.PaymentMethod.Should().Be(PaymentMethod.Card);
        booking.IsPaid.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithSerialTicketPayment_Throws()
    {
        var act = () => CreateBooking(paymentMethod: PaymentMethod.SerialTicket);

        act.Should().Throw<DomainException>().WithMessage("*not available*");
    }

    [Fact]
    public void Constructor_WithEmptyBookerId_Throws()
    {
        var act = () => new Booking(ResourceId, Guid.Empty, Now.AddHours(1), Now.AddHours(2), PaymentMethod.Cash, Now);

        act.Should().Throw<DomainException>().WithMessage("*Booker is required*");
    }

    [Fact]
    public void Constructor_WithStartInThePastRelativeToNow_Throws()
    {
        // Start is one hour before "now" — this is the case that used to be checked against
        // DateTime.UtcNow and broke for customers in timezones behind UTC. Passing "now" explicitly
        // means this test is deterministic regardless of when or where it actually runs.
        var start = Now.AddHours(-1);
        var end = Now.AddHours(1);

        var act = () => CreateBooking(start, end);

        act.Should().Throw<DomainException>().WithMessage("*in the past*");
    }

    [Fact]
    public void Constructor_WithStartExactlyAtNow_DoesNotThrow()
    {
        var act = () => CreateBooking(Now, Now.AddMinutes(30));

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithStartAfterEnd_Throws()
    {
        var start = Now.AddHours(2);
        var end = Now.AddHours(1);

        var act = () => CreateBooking(start, end);

        act.Should().Throw<DomainException>().WithMessage("*before its end*");
    }

    [Fact]
    public void OverlapsWith_WhenRangesOverlap_ReturnsTrue()
    {
        var booking = CreateBooking(Now.AddHours(1), Now.AddHours(2));

        booking.OverlapsWith(Now.AddHours(1).AddMinutes(30), Now.AddHours(3)).Should().BeTrue();
    }

    [Fact]
    public void OverlapsWith_WhenRangesDoNotTouch_ReturnsFalse()
    {
        var booking = CreateBooking(Now.AddHours(1), Now.AddHours(2));

        booking.OverlapsWith(Now.AddHours(2), Now.AddHours(3)).Should().BeFalse();
    }

    [Fact]
    public void OverlapsWith_WhenBookingIsCancelled_ReturnsFalse()
    {
        var booking = CreateBooking(Now.AddHours(1), Now.AddHours(2), PaymentMethod.Cash);
        booking.Cancel();

        booking.OverlapsWith(Now.AddHours(1), Now.AddHours(2)).Should().BeFalse();
    }

    [Fact]
    public void OverlapsWith_WhenBookingIsReady_StillReturnsTrue()
    {
        var start = Now.AddHours(1);
        var booking = CreateBooking(start, start.AddHours(1));
        booking.CheckIn(start.AddMinutes(-10));

        booking.OverlapsWith(start, start.AddHours(1)).Should().BeTrue();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var booking = CreateBooking(Now.AddHours(1), Now.AddHours(2), PaymentMethod.Cash);
        booking.Cancel();

        var act = () => booking.Cancel();

        act.Should().Throw<DomainException>().WithMessage("*already cancelled*");
    }

    [Fact]
    public void Cancel_WhenPaid_Throws()
    {
        var booking = CreateBooking(paymentMethod: PaymentMethod.Card);

        var act = () => booking.Cancel();

        act.Should().Throw<DomainException>().WithMessage("*unpaid*");
    }

    [Fact]
    public void Cancel_WhenUnpaidCash_Succeeds()
    {
        var booking = CreateBooking(paymentMethod: PaymentMethod.Cash);

        booking.Cancel();

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void CheckIn_WithinWindow_SetsStatusToReady()
    {
        var start = Now.AddHours(1);
        var booking = CreateBooking(start, start.AddHours(1));

        booking.CheckIn(start.AddMinutes(-10));

        booking.Status.Should().Be(BookingStatus.Ready);
    }

    [Fact]
    public void CheckIn_ExactlyAtWindowOpen_DoesNotThrow()
    {
        var start = Now.AddHours(1);
        var booking = CreateBooking(start, start.AddHours(1));

        var act = () => booking.CheckIn(start.AddMinutes(-Booking.CheckInWindowMinutes));

        act.Should().NotThrow();
    }

    [Fact]
    public void CheckIn_TooEarly_Throws()
    {
        var start = Now.AddHours(1);
        var booking = CreateBooking(start, start.AddHours(1));

        var act = () => booking.CheckIn(start.AddMinutes(-20));

        act.Should().Throw<DomainException>().WithMessage("*Check-in opens*");
    }

    [Fact]
    public void CheckIn_AfterBookingEnded_Throws()
    {
        var start = Now.AddHours(1);
        var end = start.AddHours(1);
        var booking = CreateBooking(start, end);

        var act = () => booking.CheckIn(end.AddMinutes(1));

        act.Should().Throw<DomainException>().WithMessage("*already ended*");
    }

    [Fact]
    public void CheckIn_WhenNotPending_Throws()
    {
        var start = Now.AddHours(1);
        var booking = CreateBooking(start, start.AddHours(1));
        booking.CheckIn(start.AddMinutes(-10));

        var act = () => booking.CheckIn(start.AddMinutes(-5));

        act.Should().Throw<DomainException>().WithMessage("*Only a pending booking*");
    }

    [Fact]
    public void MarkPaid_WhenPending_SetsToPaid()
    {
        var booking = CreateBooking(paymentMethod: PaymentMethod.Cash);

        booking.MarkPaid();

        booking.IsPaid.Should().BeTrue();
    }

    [Fact]
    public void MarkPaid_WhenAlreadyPaid_Throws()
    {
        var booking = CreateBooking(paymentMethod: PaymentMethod.Card);

        var act = () => booking.MarkPaid();

        act.Should().Throw<DomainException>().WithMessage("*already paid*");
    }

    [Fact]
    public void MarkPaid_WhenCancelled_Throws()
    {
        var booking = CreateBooking(paymentMethod: PaymentMethod.Cash);
        booking.Cancel();

        var act = () => booking.MarkPaid();

        act.Should().Throw<DomainException>().WithMessage("*cancelled*");
    }

    [Fact]
    public void Reschedule_ToValidSlot_UpdatesResourceAndTimes()
    {
        var booking = CreateBooking();
        var newResourceId = Guid.NewGuid();
        var newStart = Now.AddHours(3);
        var newEnd = Now.AddHours(4);

        booking.Reschedule(newResourceId, newStart, newEnd, Now);

        booking.ResourceId.Should().Be(newResourceId);
        booking.Start.Should().Be(newStart);
        booking.End.Should().Be(newEnd);
    }

    [Fact]
    public void Reschedule_WhenCancelled_Throws()
    {
        var booking = CreateBooking(paymentMethod: PaymentMethod.Cash);
        booking.Cancel();

        var act = () => booking.Reschedule(ResourceId, Now.AddHours(3), Now.AddHours(4), Now);

        act.Should().Throw<DomainException>().WithMessage("*Only a pending booking*");
    }

    [Fact]
    public void Reschedule_WhenCheckedIn_Throws()
    {
        var start = Now.AddHours(1);
        var booking = CreateBooking(start, start.AddHours(1));
        booking.CheckIn(start.AddMinutes(-10));

        var act = () => booking.Reschedule(ResourceId, Now.AddHours(5), Now.AddHours(6), Now);

        act.Should().Throw<DomainException>().WithMessage("*Only a pending booking*");
    }

    [Fact]
    public void Reschedule_WithStartAfterEnd_Throws()
    {
        var booking = CreateBooking();

        var act = () => booking.Reschedule(ResourceId, Now.AddHours(4), Now.AddHours(3), Now);

        act.Should().Throw<DomainException>().WithMessage("*before its end*");
    }

    [Fact]
    public void Reschedule_ToPastSlot_Throws()
    {
        var booking = CreateBooking();

        var act = () => booking.Reschedule(ResourceId, Now.AddHours(-2), Now.AddHours(-1), Now);

        act.Should().Throw<DomainException>().WithMessage("*in the past*");
    }
}

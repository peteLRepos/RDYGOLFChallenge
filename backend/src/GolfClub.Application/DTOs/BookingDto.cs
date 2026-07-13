using GolfClub.Domain.Enums;

namespace GolfClub.Application.DTOs;

public record BookingDto(
    Guid Id,
    Guid ResourceId,
    string ResourceName,
    Guid BookerId,
    string CustomerName,
    string CustomerEmail,
    DateTime Start,
    DateTime End,
    PaymentMethod PaymentMethod,
    bool IsPaid,
    BookingStatus Status,
    int PlayerCount,
    int CombinedHandicap,
    List<BookingPlayerDto> Players,
    decimal TotalPrice,
    DateTime CreatedAt);

public record BookingPlayerDto(
    Guid UserId,
    string Name,
    int Handicap);

public record CreateBookingRequest(
    Guid ResourceId,
    DateTime Start,
    DateTime End,
    PaymentMethod PaymentMethod);

public record MoveBookingRequest(
    Guid ResourceId,
    DateTime Start,
    DateTime End);

public record AddPlayerRequest(Guid UserId);

public record TimeSlotDto(
    DateTime Start,
    DateTime End,
    bool IsAvailable);

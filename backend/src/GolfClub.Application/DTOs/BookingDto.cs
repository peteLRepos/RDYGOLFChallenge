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
    DateTime CreatedAt);

public record CreateBookingRequest(
    Guid ResourceId,
    DateTime Start,
    DateTime End,
    PaymentMethod PaymentMethod);

public record MoveBookingRequest(
    Guid ResourceId,
    DateTime Start,
    DateTime End);

public record TimeSlotDto(
    DateTime Start,
    DateTime End,
    bool IsAvailable);

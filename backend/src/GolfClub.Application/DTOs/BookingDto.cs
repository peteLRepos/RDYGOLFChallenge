using GolfClub.Domain.Enums;

namespace GolfClub.Application.DTOs;

public record BookingDto(
    Guid Id,
    Guid ResourceId,
    string ResourceName,
    DateTime Start,
    DateTime End,
    string CustomerName,
    string CustomerEmail,
    BookingStatus Status,
    DateTime CreatedAt);

public record CreateBookingRequest(
    Guid ResourceId,
    DateTime Start,
    DateTime End,
    string CustomerName,
    string CustomerEmail);

public record TimeSlotDto(
    DateTime Start,
    DateTime End,
    bool IsAvailable);

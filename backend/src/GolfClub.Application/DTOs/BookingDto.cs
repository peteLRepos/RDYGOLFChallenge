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
    int Handicap,
    PaymentMethod PaymentMethod,
    Guid AddedByUserId);

/// <param name="Players">
/// The full player list for the booking, in slot order. The first entry must be the requesting
/// user (they're always their own booking's first player) — enforced in BookingService, not here.
/// </param>
public record CreateBookingRequest(
    Guid ResourceId,
    DateTime Start,
    DateTime End,
    List<PlayerSelectionDto> Players);

public record PlayerSelectionDto(Guid UserId, PaymentMethod PaymentMethod);

public record MoveBookingRequest(
    Guid ResourceId,
    DateTime Start,
    DateTime End);

public record AddPlayerRequest(Guid UserId, PaymentMethod PaymentMethod);

/// <param name="BookingId">
/// Only set when the slot isn't available — lets the public booking flow join an in-progress
/// booking (fewer than 4 players) without ever seeing who's already in it (no names/emails here).
/// </param>
public record TimeSlotDto(
    DateTime Start,
    DateTime End,
    bool IsAvailable,
    Guid? BookingId,
    int? PlayerCount,
    int? CombinedHandicap);

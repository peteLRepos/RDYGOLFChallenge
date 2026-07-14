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
    DateTime CreatedAt,
    Guid? CartId,
    string? CartName);

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
/// <param name="WantsCart">If true, a free cart is attached at creation, or the whole request fails
/// with "No carts available." if none are free for the booking's 2-hour cart window.</param>
public record CreateBookingRequest(
    Guid ResourceId,
    DateTime Start,
    DateTime End,
    List<PlayerSelectionDto> Players,
    bool WantsCart = false);

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

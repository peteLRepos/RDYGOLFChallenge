using GolfClub.Application.DTOs;
using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _carts;
    private readonly IBookingRepository _bookings;
    private readonly IUnitOfWork _unitOfWork;

    public CartService(ICartRepository carts, IBookingRepository bookings, IUnitOfWork unitOfWork)
    {
        _carts = carts;
        _bookings = bookings;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CartDto>> GetAllAsync(CancellationToken ct = default)
    {
        var carts = await _carts.GetAllAsync(ct);
        return carts.Select(ToDto).ToList();
    }

    public async Task<CartDto> CreateAsync(CreateCartRequest request, CancellationToken ct = default)
    {
        var cart = new Cart(request.Name);

        await _carts.AddAsync(cart, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto(cart);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var cart = await _carts.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Cart '{id}' was not found.");

        if (isActive)
            cart.Activate();
        else
            cart.Deactivate();

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var cart = await _carts.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Cart '{id}' was not found.");

        // A cart that's ever been linked to a booking (even a cancelled one) keeps that history —
        // disable it instead of deleting if it shouldn't be offered anymore.
        if (await _bookings.HasCartReferenceAsync(id, ct))
            throw new DomainException("This cart is linked to a booking and cannot be deleted. Disable it instead.");

        _carts.Remove(cart);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<bool> IsAvailableAsync(DateTime start, CancellationToken ct = default)
    {
        var freeCartId = await TryFindAvailableCartIdAsync(start, ct);
        return freeCartId.HasValue;
    }

    public async Task<Guid> FindAvailableCartIdAsync(DateTime start, CancellationToken ct = default) =>
        await TryFindAvailableCartIdAsync(start, ct)
            ?? throw new DomainException("No carts available.");

    private async Task<Guid?> TryFindAvailableCartIdAsync(DateTime start, CancellationToken ct)
    {
        var end = start.AddHours(Cart.ReservationHours);
        var reservedCartIds = await _bookings.GetReservedCartIdsOverlappingAsync(start, end, ct);
        var carts = await _carts.GetAllAsync(ct);

        return carts.FirstOrDefault(c => c.IsActive && !reservedCartIds.Contains(c.Id))?.Id;
    }

    private static CartDto ToDto(Cart cart) => new(cart.Id, cart.Name, cart.IsActive);
}

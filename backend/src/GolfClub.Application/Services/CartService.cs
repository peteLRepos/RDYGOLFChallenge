using GolfClub.Application.DTOs;
using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;

namespace GolfClub.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _carts;
    private readonly IUnitOfWork _unitOfWork;

    public CartService(ICartRepository carts, IUnitOfWork unitOfWork)
    {
        _carts = carts;
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

        _carts.Remove(cart);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static CartDto ToDto(Cart cart) => new(cart.Id, cart.Name, cart.IsActive);
}

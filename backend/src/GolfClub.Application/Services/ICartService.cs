using GolfClub.Application.DTOs;

namespace GolfClub.Application.Services;

public interface ICartService
{
    Task<List<CartDto>> GetAllAsync(CancellationToken ct = default);
    Task<CartDto> CreateAsync(CreateCartRequest request, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Whether at least one active cart is free for the whole 2-hour window starting at <paramref name="start"/>.</summary>
    Task<bool> IsAvailableAsync(DateTime start, CancellationToken ct = default);

    /// <summary>Picks a free cart's id for the 2-hour window starting at <paramref name="start"/>, or throws if none are available.</summary>
    Task<Guid> FindAvailableCartIdAsync(DateTime start, CancellationToken ct = default);
}

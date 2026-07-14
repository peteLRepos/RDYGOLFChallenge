using GolfClub.Application.DTOs;

namespace GolfClub.Application.Services;

public interface ICartService
{
    Task<List<CartDto>> GetAllAsync(CancellationToken ct = default);
    Task<CartDto> CreateAsync(CreateCartRequest request, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

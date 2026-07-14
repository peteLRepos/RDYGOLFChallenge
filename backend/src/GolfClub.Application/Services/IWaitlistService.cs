using GolfClub.Application.DTOs;

namespace GolfClub.Application.Services;

public interface IWaitlistService
{
    Task<WaitlistEntryDto> JoinAsync(Guid resourceId, DateTime slotStart, Guid userId, CancellationToken ct = default);
    Task LeaveAsync(Guid entryId, Guid requestingUserId, bool isAdmin, CancellationToken ct = default);
    Task<List<WaitlistEntryDto>> GetAllAsync(CancellationToken ct = default);
}

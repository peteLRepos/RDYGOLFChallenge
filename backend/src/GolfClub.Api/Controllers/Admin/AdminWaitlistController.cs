using GolfClub.Application.Interfaces;
using GolfClub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GolfClub.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/waitlist")]
[Authorize(Roles = "Admin")]
public class AdminWaitlistController : ControllerBase
{
    private readonly IWaitlistService _waitlistService;
    private readonly ICurrentUserService _currentUser;

    public AdminWaitlistController(IWaitlistService waitlistService, ICurrentUserService currentUser)
    {
        _waitlistService = waitlistService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var entries = await _waitlistService.GetAllAsync(ct);
        return Ok(entries);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        await _waitlistService.LeaveAsync(id, _currentUser.UserId, isAdmin: true, ct);
        return NoContent();
    }
}

using GolfClub.Application.DTOs;
using GolfClub.Application.Interfaces;
using GolfClub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GolfClub.Api.Controllers;

[ApiController]
[Route("api/waitlist")]
[Authorize]
public class WaitlistController : ControllerBase
{
    private readonly IWaitlistService _waitlistService;
    private readonly ICurrentUserService _currentUser;

    public WaitlistController(IWaitlistService waitlistService, ICurrentUserService currentUser)
    {
        _waitlistService = waitlistService;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Join([FromBody] JoinWaitlistRequest request, CancellationToken ct)
    {
        var entry = await _waitlistService.JoinAsync(request.ResourceId, request.SlotStart, _currentUser.UserId, ct);
        return StatusCode(StatusCodes.Status201Created, entry);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct)
    {
        await _waitlistService.LeaveAsync(id, _currentUser.UserId, _currentUser.IsAdmin, ct);
        return NoContent();
    }
}

using GolfClub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GolfClub.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserService _userService;

    public AdminUsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _userService.GetAllAsync(ct);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetByIdAsync(id, ct);
        return Ok(user);
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] bool isActive, CancellationToken ct)
    {
        await _userService.SetActiveAsync(id, isActive, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/admin")]
    public async Task<IActionResult> SetAdmin(Guid id, [FromBody] bool isAdmin, CancellationToken ct)
    {
        await _userService.SetAdminAsync(id, isAdmin, ct);
        return NoContent();
    }
}

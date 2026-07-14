using GolfClub.Application.DTOs;
using GolfClub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GolfClub.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken ct)
    {
        var result = await _userService.RegisterAsync(request, ct);
        // 201 without a Location header: there's no public GET-by-id endpoint to point at (full user
        // detail is admin-only, see UserSearchResultDto), and the response body is already
        // self-sufficient — registration logs the caller in immediately.
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [Authorize]
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        var results = await _userService.SearchAsync(q, ct);
        return Ok(results);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await _userService.ForgotPasswordAsync(request, ct);
        return Ok(result);
    }
}

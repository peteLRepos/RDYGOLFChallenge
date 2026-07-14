using GolfClub.Application.DTOs;
using GolfClub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GolfClub.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/carts")]
[Authorize(Roles = "Admin")]
public class AdminCartsController : ControllerBase
{
    private readonly ICartService _cartService;

    public AdminCartsController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var carts = await _cartService.GetAllAsync(ct);
        return Ok(carts);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCartRequest request, CancellationToken ct)
    {
        var cart = await _cartService.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, cart);
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] bool isActive, CancellationToken ct)
    {
        await _cartService.SetActiveAsync(id, isActive, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _cartService.DeleteAsync(id, ct);
        return NoContent();
    }
}

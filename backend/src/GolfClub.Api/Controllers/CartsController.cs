using GolfClub.Application.DTOs;
using GolfClub.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GolfClub.Api.Controllers;

[ApiController]
[Route("api/carts")]
public class CartsController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartsController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>Whether a cart is free for the 2-hour window starting at <paramref name="start"/> —
    /// public and unauthenticated, same as resource availability, so the booking dialog can grey out
    /// the cart option before the user is even logged in.</summary>
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability([FromQuery] DateTime start, CancellationToken ct)
    {
        var isAvailable = await _cartService.IsAvailableAsync(start, ct);
        return Ok(new CartAvailabilityDto(isAvailable));
    }
}

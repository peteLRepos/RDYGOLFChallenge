using GolfClub.Application.DTOs;
using GolfClub.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GolfClub.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        var booking = await _bookingService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Create), new { id = booking.Id }, booking);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _bookingService.CancelAsync(id, ct);
        return NoContent();
    }
}

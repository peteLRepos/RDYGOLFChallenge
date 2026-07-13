using GolfClub.Application.DTOs;
using GolfClub.Application.Interfaces;
using GolfClub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GolfClub.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/bookings")]
[Authorize(Roles = "Admin")]
public class AdminBookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ICurrentUserService _currentUser;

    public AdminBookingsController(IBookingService bookingService, ICurrentUserService currentUser)
    {
        _bookingService = bookingService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var bookings = await _bookingService.GetAllAsync(ct);
        return Ok(bookings);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _bookingService.CancelAsync(id, _currentUser.UserId, isAdmin: true, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/checkin")]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken ct)
    {
        await _bookingService.CheckInAsync(id, _currentUser.UserId, isAdmin: true, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken ct)
    {
        await _bookingService.MarkPaidAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveBookingRequest request, CancellationToken ct)
    {
        var booking = await _bookingService.MoveAsync(id, request, ct);
        return Ok(booking);
    }
}

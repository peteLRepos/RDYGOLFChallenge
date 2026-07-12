using GolfClub.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GolfClub.Api.Controllers;

[ApiController]
[Route("api/resources")]
public class ResourcesController : ControllerBase
{
    private readonly IResourceService _resourceService;
    private readonly IBookingService _bookingService;

    public ResourcesController(IResourceService resourceService, IBookingService bookingService)
    {
        _resourceService = resourceService;
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var resources = await _resourceService.GetAllAsync(includeInactive: false, ct);
        return Ok(resources);
    }

    [HttpGet("{id:guid}/availability")]
    public async Task<IActionResult> GetAvailability(Guid id, [FromQuery] DateOnly date, CancellationToken ct)
    {
        var slots = await _bookingService.GetAvailabilityAsync(id, date, ct);
        return Ok(slots);
    }
}

using GolfClub.Application.DTOs;
using GolfClub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GolfClub.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/resources")]
[Authorize(Roles = "Admin")]
public class AdminResourcesController : ControllerBase
{
    private readonly IResourceService _resourceService;

    public AdminResourcesController(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var resources = await _resourceService.GetAllAsync(includeInactive: true, ct);
        return Ok(resources);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var resource = await _resourceService.GetByIdAsync(id, ct);
        return Ok(resource);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateResourceRequest request, CancellationToken ct)
    {
        var resource = await _resourceService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = resource.Id }, resource);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateResourceRequest request, CancellationToken ct)
    {
        var resource = await _resourceService.UpdateAsync(id, request, ct);
        return Ok(resource);
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] bool isActive, CancellationToken ct)
    {
        await _resourceService.SetActiveAsync(id, isActive, ct);
        return NoContent();
    }
}

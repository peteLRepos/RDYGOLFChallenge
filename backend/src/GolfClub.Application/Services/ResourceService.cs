using GolfClub.Application.DTOs;
using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;

namespace GolfClub.Application.Services;

public class ResourceService : IResourceService
{
    private readonly IResourceRepository _resources;
    private readonly IUnitOfWork _unitOfWork;

    public ResourceService(IResourceRepository resources, IUnitOfWork unitOfWork)
    {
        _resources = resources;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ResourceDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
    {
        var resources = await _resources.GetAllAsync(includeInactive, ct);
        return resources.Select(ToDto).ToList();
    }

    public async Task<ResourceDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var resource = await _resources.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Resource '{id}' was not found.");
        return ToDto(resource);
    }

    public async Task<ResourceDto> CreateAsync(CreateResourceRequest request, CancellationToken ct = default)
    {
        var resource = new Resource(
            request.Name,
            request.Type,
            request.SlotDurationMinutes,
            request.OpeningTime,
            request.ClosingTime);

        await _resources.AddAsync(resource, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto(resource);
    }

    public async Task<ResourceDto> UpdateAsync(Guid id, UpdateResourceRequest request, CancellationToken ct = default)
    {
        var resource = await _resources.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Resource '{id}' was not found.");

        resource.Update(request.Name, request.SlotDurationMinutes, request.OpeningTime, request.ClosingTime);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto(resource);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var resource = await _resources.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Resource '{id}' was not found.");

        if (isActive)
            resource.Activate();
        else
            resource.Deactivate();

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static ResourceDto ToDto(Resource resource) => new(
        resource.Id,
        resource.Name,
        resource.Type,
        resource.SlotDurationMinutes,
        resource.OpeningTime,
        resource.ClosingTime,
        resource.IsActive);
}

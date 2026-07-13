using System.Security.Claims;
using GolfClub.Application.Interfaces;

namespace GolfClub.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            // The "sub" claim JwtTokenGenerator writes gets remapped to the long ClaimTypes URI by
            // the JwtBearer pipeline's default inbound claim mapping — confirmed against a real
            // issued token through the actual running API (a synthetic JsonWebTokenHandler-only
            // test doesn't reproduce this, since the remapping happens elsewhere in the ASP.NET
            // Core hosting pipeline, not in token validation itself).
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException(
                    $"No '{ClaimTypes.NameIdentifier}' claim on the current user — this property " +
                    "should only be accessed behind [Authorize].");

            return Guid.Parse(claim.Value);
        }
    }

    public bool IsAdmin => User.IsInRole("Admin");

    // Both properties document the same precondition (only valid behind [Authorize]), so both
    // fail the same way if that precondition is violated — an IsAdmin that quietly defaulted to
    // "not admin" on a missing HttpContext would under-authorize silently instead of surfacing
    // the misuse the way UserId does.
    private ClaimsPrincipal User =>
        _httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException(
                "No HttpContext available — this property should only be accessed behind [Authorize].");
}

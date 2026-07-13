using System.IdentityModel.Tokens.Jwt;
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
            // .NET 8's JwtBearerHandler uses JsonWebTokenHandler by default, which — unlike the
            // legacy JwtSecurityTokenHandler — does not remap short JWT claim names to long
            // ClaimTypes URIs, so the "sub" claim JwtTokenGenerator writes stays literally "sub"
            // on the resulting ClaimsPrincipal (verified against a real issued token).
            var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)
                ?? throw new InvalidOperationException(
                    $"No '{JwtRegisteredClaimNames.Sub}' claim on the current user — this property " +
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

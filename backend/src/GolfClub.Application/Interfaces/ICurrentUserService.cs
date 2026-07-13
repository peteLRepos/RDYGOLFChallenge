namespace GolfClub.Application.Interfaces;

/// <summary>
/// Reads the authenticated caller's identity out of the current request's JWT claims. Implemented
/// in the Api layer (not Infrastructure) since it depends on the current HTTP request, which is a
/// presentation-layer concern — see JwtTokenGenerator (Infrastructure) for the inverse: issuing a
/// token needs no HttpContext, so it stays there.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsAdmin { get; }
}

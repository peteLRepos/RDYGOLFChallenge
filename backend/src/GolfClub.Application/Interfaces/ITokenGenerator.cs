using GolfClub.Domain.Entities;

namespace GolfClub.Application.Interfaces;

/// <summary>
/// Abstracts JWT issuance so Application never depends on a specific token library.
/// </summary>
public interface ITokenGenerator
{
    string GenerateToken(User user);
}

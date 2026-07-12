using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GolfClub.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly GolfClubDbContext _context;

    public UnitOfWork(GolfClubDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Translates the check-then-act race condition backstop (a unique index catching a
            // duplicate that slipped past an application-level check) into a generic exception
            // Application/Domain can handle without knowing about EF Core or Npgsql.
            throw new ConflictException("A record with a conflicting unique value already exists.");
        }
    }
}

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

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            await operation();
            await transaction.CommitAsync(ct);
        });
    }

    public Task AcquireExclusiveLockAsync(long lockId, CancellationToken ct = default) =>
        // Postgres advisory lock: blocks until any other session holding the same lockId commits
        // or rolls back its transaction (pg_advisory_xact_lock is auto-released at transaction
        // end and cannot be released explicitly, which is exactly the "held for this critical
        // section only" behavior we want).
        _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockId})", ct);
}

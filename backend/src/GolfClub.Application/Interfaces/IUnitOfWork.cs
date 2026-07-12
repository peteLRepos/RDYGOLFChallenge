namespace GolfClub.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a single database transaction, committing on
    /// success and rolling back on any exception.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct = default);

    /// <summary>
    /// Acquires an exclusive, transaction-scoped lock identified by <paramref name="lockId"/> —
    /// blocks until any other holder releases it (by committing/rolling back its transaction).
    /// Must be called from within an active transaction (see <see cref="ExecuteInTransactionAsync"/>).
    /// Used to serialize a critical section that spans multiple rows, where no single-row DB
    /// constraint could otherwise enforce the invariant.
    /// </summary>
    Task AcquireExclusiveLockAsync(long lockId, CancellationToken ct = default);
}

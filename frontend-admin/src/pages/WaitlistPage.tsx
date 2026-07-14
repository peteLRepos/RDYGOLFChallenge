import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../api/client';
import type { WaitlistEntry } from '../api/types';
import { formatTime } from '../utils/date';
import './WaitlistPage.css';

export function WaitlistPage() {
  const [entries, setEntries] = useState<WaitlistEntry[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [pendingId, setPendingId] = useState<string | null>(null);

  const load = useCallback(() => {
    setIsLoading(true);
    setError(null);
    api
      .get<WaitlistEntry[]>('/api/admin/waitlist')
      .then(setEntries)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load the waitlist.'))
      .finally(() => setIsLoading(false));
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const removeEntry = async (entry: WaitlistEntry) => {
    setActionError(null);
    setPendingId(entry.id);
    try {
      await api.delete(`/api/admin/waitlist/${entry.id}`);
      load();
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not remove this entry.');
    } finally {
      setPendingId(null);
    }
  };

  return (
    <main className="page">
      <h1 className="waitlist-title">Waitlist</h1>
      <p className="subtitle">Everyone queued for a slot that's currently full, oldest first.</p>

      {isLoading && <p>Loading waitlist…</p>}
      {error && <p className="error">{error}</p>}
      {actionError && <p className="error">{actionError}</p>}
      {!isLoading && !error && entries.length === 0 && <p>No one is on the waitlist right now.</p>}

      <ul className="waitlist-list">
        {entries.map((entry) => (
          <li key={entry.id} className="waitlist-row">
            <div className="waitlist-info">
              <span className="waitlist-slot">
                {entry.resourceName} · {formatTime(entry.slotStart)}
              </span>
              <span className="waitlist-user">
                {entry.userName} ({entry.userEmail})
              </span>
            </div>
            <button
              type="button"
              className="waitlist-remove"
              disabled={pendingId === entry.id}
              onClick={() => removeEntry(entry)}
            >
              Remove
            </button>
          </li>
        ))}
      </ul>
    </main>
  );
}

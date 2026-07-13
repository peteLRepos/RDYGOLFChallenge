import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, ApiError } from '../api/client';
import type { Resource, TimeSlot } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { DateNav } from '../components/DateNav';
import { BookingDialog } from '../components/BookingDialog';
import { AuthModals, type AuthMode } from '../components/AuthModals';
import { MAX_PLAYERS } from '../constants';
import { formatTime, startOfToday, toDateKey } from '../utils/date';
import './TeeSheetPage.css';

export function TeeSheetPage() {
  const { resourceId } = useParams<{ resourceId: string }>();
  const { isAuthenticated } = useAuth();
  const [resource, setResource] = useState<Resource | null>(null);
  const [slots, setSlots] = useState<TimeSlot[]>([]);
  const [date, setDate] = useState(startOfToday());
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedSlot, setSelectedSlot] = useState<TimeSlot | null>(null);
  const [authMode, setAuthMode] = useState<AuthMode | null>(null);

  useEffect(() => {
    api
      .get<Resource[]>('/api/resources')
      .then((all) => setResource(all.find((r) => r.id === resourceId) ?? null))
      .catch(() => setResource(null));
  }, [resourceId]);

  const loadSlots = useCallback(() => {
    if (!resourceId) return;
    setIsLoading(true);
    setError(null);
    api
      .get<TimeSlot[]>(`/api/resources/${resourceId}/availability?date=${toDateKey(date)}`)
      .then(setSlots)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load the tee sheet.'))
      .finally(() => setIsLoading(false));
  }, [resourceId, date]);

  useEffect(() => {
    loadSlots();
  }, [loadSlots]);

  const handleSlotClick = (slot: TimeSlot) => {
    const isFull = !slot.isAvailable && (slot.playerCount ?? 0) >= MAX_PLAYERS;
    if (isFull) return;

    if (!isAuthenticated) {
      setAuthMode('login');
      return;
    }
    setSelectedSlot(slot);
  };

  return (
    <main className="page">
      <p className="subtitle">
        <Link to="/">&larr; All courses</Link>
      </p>
      <h1 className="tee-sheet-title">{resource?.name ?? '…'}</h1>

      <DateNav date={date} onChange={setDate} />

      {isLoading && <p>Loading tee sheet…</p>}
      {error && <p className="error">{error}</p>}

      <ul className="slot-grid">
        {slots.map((slot) => {
          const isFull = !slot.isAvailable && (slot.playerCount ?? 0) >= MAX_PLAYERS;

          return (
            <li key={slot.start}>
              <button
                type="button"
                className={
                  'slot' + (slot.isAvailable ? ' slot-open' : isFull ? ' slot-full' : ' slot-joinable')
                }
                disabled={isFull}
                onClick={() => handleSlotClick(slot)}
              >
                <span className="slot-time">{formatTime(slot.start)}</span>
                {!slot.isAvailable && (
                  <span className="slot-status">
                    Booked {slot.playerCount}/{MAX_PLAYERS}
                    {slot.combinedHandicap != null && ` · hcp ${slot.combinedHandicap}`}
                  </span>
                )}
              </button>
            </li>
          );
        })}
      </ul>

      {selectedSlot && resource && (
        <BookingDialog
          resource={resource}
          slot={selectedSlot}
          onClose={() => setSelectedSlot(null)}
          onBooked={() => {
            setSelectedSlot(null);
            loadSlots();
          }}
        />
      )}

      {authMode && (
        <AuthModals mode={authMode} onModeChange={setAuthMode} onClose={() => setAuthMode(null)} />
      )}
    </main>
  );
}

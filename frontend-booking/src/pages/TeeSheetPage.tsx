import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, ApiError } from '../api/client';
import type { Booking, Resource, TimeSlot } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { DateNav } from '../components/DateNav';
import { BookingDialog } from '../components/BookingDialog';
import { AuthModals, type AuthMode } from '../components/AuthModals';
import { MAX_PLAYERS } from '../constants';
import { formatHourLabel, formatTime, hourKey, startOfToday, toDateKey } from '../utils/date';
import './TeeSheetPage.css';

// A cross-resource block (e.g. a lesson holding this hour of the 6-Hole Course) has no bookingId —
// there's no booking on *this* resource to join, so it's just unavailable, same as a full one.
function isSlotClickable(slot: TimeSlot): boolean {
  if (slot.isAvailable) return true;
  if (slot.bookingId === null) return false;
  return (slot.playerCount ?? 0) < MAX_PLAYERS;
}

// Only a slot with an actual, full booking on *this* resource can be queued for — matches the
// backend's JoinAsync check, which looks for a same-resource booking at MaxPlayers.
function isQueueable(slot: TimeSlot): boolean {
  return !slot.isAvailable && slot.bookingId !== null && (slot.playerCount ?? 0) >= MAX_PLAYERS;
}

// Whether the current user already has an active booking covering this exact slot — checked
// against the *time range*, not just an exact start match, since a multi-hour Simulator booking
// covers several grid slots under one bookingId. TimeSlotDto never reveals who's in a booking (see
// README), so this is the only way the tee sheet can know "it's me" rather than a stranger, and
// it's what stops the Join dialog from ever opening on your own slot and re-submitting yourself as
// a second player (the backend already rejects that, but only after a confusing round trip).
function isAlreadyMine(slot: TimeSlot, resourceId: string, myBookings: Booking[]): boolean {
  const slotStart = new Date(slot.start).getTime();
  return myBookings.some(
    (b) =>
      b.resourceId === resourceId &&
      b.status !== 'Cancelled' &&
      slotStart >= new Date(b.start).getTime() &&
      slotStart < new Date(b.end).getTime(),
  );
}

// One row per hour, however many sub-hourly slots fall in it — a 10-minute-slot course gets six
// slots per row, a 60-minute one gets exactly one, no special-casing needed either way.
function groupSlotsByHour(slots: TimeSlot[]): { hour: string; slots: TimeSlot[] }[] {
  const groups: { hour: string; slots: TimeSlot[] }[] = [];
  for (const slot of slots) {
    const key = hourKey(slot.start);
    const current = groups.at(-1);
    if (current?.hour === key) {
      current.slots.push(slot);
    } else {
      groups.push({ hour: key, slots: [slot] });
    }
  }
  return groups;
}

export function TeeSheetPage() {
  const { resourceId } = useParams<{ resourceId: string }>();
  const { isAuthenticated } = useAuth();
  const [resource, setResource] = useState<Resource | null>(null);
  const [isResourceLoading, setIsResourceLoading] = useState(true);
  const [slots, setSlots] = useState<TimeSlot[]>([]);
  const [date, setDate] = useState(startOfToday());
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedSlot, setSelectedSlot] = useState<TimeSlot | null>(null);
  const [authMode, setAuthMode] = useState<AuthMode | null>(null);
  const [queuedSlotStarts, setQueuedSlotStarts] = useState<Set<string>>(new Set());
  const [myBookings, setMyBookings] = useState<Booking[]>([]);

  useEffect(() => {
    setIsResourceLoading(true);
    api
      .get<Resource[]>('/api/resources')
      .then((all) => setResource(all.find((r) => r.id === resourceId) ?? null))
      .catch(() => setResource(null))
      .finally(() => setIsResourceLoading(false));
  }, [resourceId]);

  const loadMyBookings = useCallback(() => {
    if (!isAuthenticated) {
      setMyBookings([]);
      return;
    }
    api
      .get<Booking[]>('/api/bookings/mine')
      .then(setMyBookings)
      .catch(() => setMyBookings([]));
  }, [isAuthenticated]);

  useEffect(() => {
    loadMyBookings();
  }, [loadMyBookings]);

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
    if (!isSlotClickable(slot)) return;

    if (!isAuthenticated) {
      setAuthMode('login');
      return;
    }

    setError(null);
    if (!slot.isAvailable && resourceId && isAlreadyMine(slot, resourceId, myBookings)) {
      setError("You're already booked into this slot.");
      return;
    }
    setSelectedSlot(slot);
  };

  const handleJoinQueue = async (slot: TimeSlot) => {
    if (!isAuthenticated) {
      setAuthMode('login');
      return;
    }
    if (!resourceId) return;

    setError(null);
    try {
      await api.post('/api/waitlist', { resourceId, slotStart: slot.start });
      setQueuedSlotStarts((prev) => new Set(prev).add(slot.start));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not join the queue.');
    }
  };

  return (
    <main className="page">
      <p className="subtitle">
        <Link to="/">&larr; All courses</Link>
      </p>
      <h1 className="tee-sheet-title">{resource?.name ?? (isResourceLoading ? '…' : 'Course not found')}</h1>

      {!isResourceLoading && !resource && (
        <p className="error">This course doesn't exist or is no longer available.</p>
      )}

      {resource && (
        <>
          <DateNav date={date} onChange={setDate} />

          {isLoading && <p>Loading tee sheet…</p>}
          {error && <p className="error">{error}</p>}
          {!isLoading && !error && slots.length === 0 && <p>No time slots are configured for this course.</p>}

          <div className="hour-rows">
            {groupSlotsByHour(slots).map((group) => (
              <div className="hour-row" key={group.hour}>
                {group.slots.length > 1 && (
                  <span className="hour-label">{formatHourLabel(group.slots[0].start)}</span>
                )}
                <ul className="slot-grid">
                  {group.slots.map((slot) => {
                    const clickable = isSlotClickable(slot);
                    const queueable = isQueueable(slot);
                    const isQueued = queuedSlotStarts.has(slot.start);

                    return (
                      <li key={slot.start}>
                        <button
                          type="button"
                          className={
                            'slot' + (slot.isAvailable ? ' slot-open' : clickable ? ' slot-joinable' : ' slot-full')
                          }
                          disabled={!clickable}
                          onClick={() => handleSlotClick(slot)}
                        >
                          <span className="slot-time">{formatTime(slot.start)}</span>
                          {!slot.isAvailable && (
                            <span className="slot-status">
                              {slot.bookingId === null ? (
                                'Unavailable'
                              ) : (
                                <>
                                  Booked {slot.playerCount}/{MAX_PLAYERS}
                                  {slot.combinedHandicap != null && ` · hcp ${slot.combinedHandicap}`}
                                </>
                              )}
                            </span>
                          )}
                        </button>
                        {queueable && (
                          <button
                            type="button"
                            className="slot-queue-btn"
                            disabled={isQueued}
                            onClick={() => handleJoinQueue(slot)}
                          >
                            {isQueued ? "You're queued" : 'Add me to queue'}
                          </button>
                        )}
                      </li>
                    );
                  })}
                </ul>
              </div>
            ))}
          </div>

          {selectedSlot && (
            <BookingDialog
              resource={resource}
              slot={selectedSlot}
              onClose={() => setSelectedSlot(null)}
              onBooked={() => {
                setSelectedSlot(null);
                loadSlots();
                loadMyBookings();
              }}
            />
          )}
        </>
      )}

      {authMode && (
        <AuthModals mode={authMode} onModeChange={setAuthMode} onClose={() => setAuthMode(null)} />
      )}
    </main>
  );
}

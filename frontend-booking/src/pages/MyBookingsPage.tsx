import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../api/client';
import type { Booking } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { CHECK_IN_WINDOW_MINUTES } from '../constants';
import { formatDateTime } from '../utils/date';
import './MyBookingsPage.css';

export function MyBookingsPage() {
  const { user, isAuthenticated } = useAuth();
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [actionError, setActionError] = useState<string | null>(null);
  const [pendingId, setPendingId] = useState<string | null>(null);

  const load = useCallback(() => {
    if (!isAuthenticated) return;
    setIsLoading(true);
    setError(null);
    api
      .get<Booking[]>('/api/bookings/mine')
      .then((all) => setBookings([...all].sort((a, b) => a.start.localeCompare(b.start))))
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load your bookings.'))
      .finally(() => setIsLoading(false));
  }, [isAuthenticated]);

  useEffect(() => {
    load();
  }, [load]);

  if (!isAuthenticated) {
    return (
      <main className="page">
        <h1 className="my-bookings-title">My Bookings</h1>
        <p>Log in to see your bookings.</p>
      </main>
    );
  }

  const runAction = async (bookingId: string, action: () => Promise<unknown>) => {
    setActionError(null);
    setPendingId(bookingId);
    try {
      await action();
      load();
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Action failed.');
    } finally {
      setPendingId(null);
    }
  };

  const cancelBooking = (id: string) => runAction(id, () => api.delete(`/api/bookings/${id}`));
  const unbookMe = (id: string) => runAction(id, () => api.delete(`/api/bookings/${id}/players/${user!.id}`));
  const checkIn = (id: string) => runAction(id, () => api.post(`/api/bookings/${id}/checkin`));

  return (
    <main className="page">
      <h1 className="my-bookings-title">My Bookings</h1>

      {isLoading && <p>Loading your bookings…</p>}
      {error && <p className="error">{error}</p>}
      {actionError && <p className="error">{actionError}</p>}
      {!isLoading && bookings.length === 0 && <p>You don't have any bookings yet.</p>}

      <ul className="booking-list">
        {bookings.map((booking) => {
          const isBooker = booking.bookerId === user!.id;
          const isPending = booking.status === 'Pending';
          const isBusy = pendingId === booking.id;
          const canCheckIn =
            isBooker &&
            isPending &&
            new Date() >= new Date(new Date(booking.start).getTime() - CHECK_IN_WINDOW_MINUTES * 60_000) &&
            new Date() < new Date(booking.end);

          return (
            <li key={booking.id} className="booking-card">
              <div className="booking-card-header">
                <div>
                  <h2>{booking.resourceName}</h2>
                  <p className="booking-when">{formatDateTime(booking.start)}</p>
                </div>
                <span className={'booking-status status-' + booking.status.toLowerCase()}>
                  {booking.status}
                </span>
              </div>

              <ul className="booking-players">
                {booking.players.map((player) => (
                  <li key={player.userId}>
                    {player.name} · hcp {player.handicap} · {player.paymentMethod}
                    {player.userId === booking.bookerId && ' (booker)'}
                  </li>
                ))}
              </ul>

              <div className="booking-card-footer">
                <span>
                  Total €{booking.totalPrice.toFixed(2)} · {booking.isPaid ? 'Paid' : 'Not paid'}
                </span>
                <div className="booking-actions">
                  {canCheckIn && (
                    <button
                      type="button"
                      className="modal-submit"
                      disabled={isBusy}
                      onClick={() => checkIn(booking.id)}
                    >
                      Check in
                    </button>
                  )}
                  {isPending && isBooker && (
                    <button
                      type="button"
                      className="booking-cancel"
                      disabled={isBusy || booking.isPaid}
                      title={booking.isPaid ? 'Paid bookings cannot be cancelled' : undefined}
                      onClick={() => cancelBooking(booking.id)}
                    >
                      Cancel booking
                    </button>
                  )}
                  {isPending && !isBooker && (
                    <button
                      type="button"
                      className="booking-cancel"
                      disabled={isBusy}
                      onClick={() => unbookMe(booking.id)}
                    >
                      Unbook me
                    </button>
                  )}
                </div>
              </div>
            </li>
          );
        })}
      </ul>
    </main>
  );
}

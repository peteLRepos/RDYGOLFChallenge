import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, ApiError } from '../api/client';
import type { Booking, Resource, UpdateResourceRequest } from '../api/types';
import { DateNav } from '../components/DateNav';
import { CHECK_IN_WINDOW_MINUTES } from '../constants';
import { formatTime, startOfToday } from '../utils/date';
import { buildSlotsForDate } from '../utils/slots';
import './TeeSheetPage.css';

export function TeeSheetPage() {
  const { resourceId } = useParams<{ resourceId: string }>();
  const [resource, setResource] = useState<Resource | null>(null);
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [date, setDate] = useState(startOfToday());
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [priceInput, setPriceInput] = useState('');
  const [isSavingPrice, setIsSavingPrice] = useState(false);
  const [pendingBookingId, setPendingBookingId] = useState<string | null>(null);

  const load = useCallback(() => {
    if (!resourceId) return;
    setIsLoading(true);
    setError(null);
    Promise.all([
      api.get<Resource>(`/api/admin/resources/${resourceId}`),
      api.get<Booking[]>('/api/admin/bookings'),
    ])
      .then(([r, b]) => {
        setResource(r);
        setPriceInput(r.pricePerPlayer != null ? String(r.pricePerPlayer) : '');
        setBookings(b);
      })
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load the tee sheet.'))
      .finally(() => setIsLoading(false));
  }, [resourceId]);

  useEffect(() => {
    load();
  }, [load]);

  const savePrice = async () => {
    if (!resource) return;
    setIsSavingPrice(true);
    setActionError(null);
    try {
      const request: UpdateResourceRequest = {
        name: resource.name,
        slotDurationMinutes: resource.slotDurationMinutes,
        openingTime: resource.openingTime,
        closingTime: resource.closingTime,
        pricePerPlayer: priceInput.trim() === '' ? null : Number(priceInput),
      };
      const updated = await api.put<Resource>(`/api/admin/resources/${resource.id}`, request);
      setResource(updated);
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not update the price.');
    } finally {
      setIsSavingPrice(false);
    }
  };

  const checkIn = async (bookingId: string) => {
    setActionError(null);
    setPendingBookingId(bookingId);
    try {
      await api.post(`/api/admin/bookings/${bookingId}/checkin`);
      load();
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Could not check in this booking.');
    } finally {
      setPendingBookingId(null);
    }
  };

  const slots = resource ? buildSlotsForDate(resource, date, bookings) : [];

  return (
    <main className="page">
      <p className="subtitle">
        <Link to="/">&larr; All resources</Link>
      </p>
      <h1 className="tee-sheet-title">{resource?.name ?? '…'}</h1>

      {resource && (
        <div className="price-editor">
          <label>
            Price per player (€)
            <input
              type="number"
              min={0}
              step="0.01"
              value={priceInput}
              onChange={(e) => setPriceInput(e.target.value)}
            />
          </label>
          <button type="button" className="price-save-button" disabled={isSavingPrice} onClick={savePrice}>
            {isSavingPrice ? 'Saving…' : 'Save price'}
          </button>
        </div>
      )}

      <DateNav date={date} onChange={setDate} />

      {isLoading && <p>Loading tee sheet…</p>}
      {error && <p className="error">{error}</p>}
      {actionError && <p className="error">{actionError}</p>}

      <ul className="slot-grid">
        {slots.map((slot) => {
          const booking = slot.booking;
          const isReady = booking?.status === 'Ready';
          const canCheckIn =
            booking &&
            !isReady &&
            new Date() >= new Date(slot.start.getTime() - CHECK_IN_WINDOW_MINUTES * 60_000) &&
            new Date() < slot.end;

          return (
            <li
              key={slot.start.toISOString()}
              className={'slot' + (booking ? (isReady ? ' slot-ready' : ' slot-booked') : ' slot-open')}
            >
              <span className="slot-time">{formatTime(slot.start.toISOString())}</span>
              {booking && (
                <>
                  <span className="slot-customer">{booking.customerName}</span>
                  <span className="slot-status">
                    {booking.playerCount}/4 · hcp {booking.combinedHandicap}
                  </span>
                  <span className={'paid-badge' + (booking.isPaid ? ' paid' : ' unpaid')}>
                    {booking.isPaid ? 'PAID' : 'NOT PAID'}
                  </span>
                  {canCheckIn && (
                    <button
                      type="button"
                      className="slot-checkin"
                      disabled={pendingBookingId === booking.id}
                      onClick={() => checkIn(booking.id)}
                    >
                      Check in
                    </button>
                  )}
                </>
              )}
              {!booking && <span className="slot-open-label">Open</span>}
            </li>
          );
        })}
      </ul>
    </main>
  );
}

import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, ApiError } from '../api/client';
import type { Booking, Resource, UpdateResourceRequest } from '../api/types';
import { DateNav } from '../components/DateNav';
import { BookingDialog } from '../components/BookingDialog';
import { CHECK_IN_WINDOW_MINUTES } from '../constants';
import { formatHourLabel, formatTime, startOfToday } from '../utils/date';
import { buildSlotsForDate, groupSlotsByHour, type AdminSlot } from '../utils/slots';
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
  const [selectedSlot, setSelectedSlot] = useState<AdminSlot | null>(null);

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

      <div className="hour-rows">
        {groupSlotsByHour(slots).map((group) => (
          <div className="hour-row" key={group.hour}>
            {group.slots.length > 1 && (
              <span className="hour-label">{formatHourLabel(group.slots[0].start)}</span>
            )}
            <ul className="slot-grid">
              {group.slots.map((slot) => {
                const booking = slot.booking;
                const isReady = booking?.status === 'Ready';
                const canCheckIn =
                  booking &&
                  !isReady &&
                  new Date() >= new Date(slot.start.getTime() - CHECK_IN_WINDOW_MINUTES * 60_000) &&
                  new Date() < slot.end;

                return (
                  <li key={slot.start.toISOString()}>
                    <button
                      type="button"
                      className={'slot' + (booking ? (isReady ? ' slot-ready' : ' slot-booked') : ' slot-open')}
                      disabled={isReady}
                      onClick={() => setSelectedSlot(slot)}
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
                            <span
                              className="slot-checkin"
                              role="button"
                              tabIndex={0}
                              onClick={(e) => {
                                e.stopPropagation();
                                checkIn(booking.id);
                              }}
                            >
                              {pendingBookingId === booking.id ? 'Checking in…' : 'Check in'}
                            </span>
                          )}
                        </>
                      )}
                      {!booking && <span className="slot-open-label">Open</span>}
                    </button>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </div>

      {selectedSlot && resource && (
        <BookingDialog
          resource={resource}
          slot={selectedSlot}
          onClose={() => setSelectedSlot(null)}
          onChanged={() => {
            setSelectedSlot(null);
            load();
          }}
        />
      )}
    </main>
  );
}

import { useState } from 'react';
import { Modal } from './Modal';
import { PlayerSearch } from './PlayerSearch';
import { api, ApiError } from '../api/client';
import { MAX_COMBINED_HANDICAP, MAX_PLAYERS } from '../constants';
import type { Booking, CreateBookingRequest, PaymentMethod, Resource, UserSearchResult } from '../api/types';
import { formatTime } from '../utils/date';
import './BookingDialog.css';

const MIN_SIMULATOR_HOURS = 1;
const MAX_SIMULATOR_HOURS = 5;

type FilledSlot = { userId: string; name: string; handicap: number; paymentMethod: PaymentMethod };

interface BookingDialogProps {
  resource: Resource;
  slot: { start: Date; end: Date; booking: Booking | null };
  onClose: () => void;
  onChanged: () => void;
}

export function BookingDialog({ resource, slot, onClose, onChanged }: BookingDialogProps) {
  return slot.booking ? (
    <ViewDialog resource={resource} booking={slot.booking} onClose={onClose} onChanged={onChanged} />
  ) : (
    <CreateDialog resource={resource} start={slot.start} end={slot.end} onClose={onClose} onChanged={onChanged} />
  );
}

function CreateDialog({
  resource,
  start,
  end,
  onClose,
  onChanged,
}: {
  resource: Resource;
  start: Date;
  end: Date;
  onClose: () => void;
  onChanged: () => void;
}) {
  const [players, setPlayers] = useState<(FilledSlot | null)[]>(Array(MAX_PLAYERS).fill(null));
  const [searchingIndex, setSearchingIndex] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [durationHours, setDurationHours] = useState(MIN_SIMULATOR_HOURS);

  const isSimulator = resource.type === 'Simulator';
  const bookingEnd = isSimulator ? new Date(start.getTime() + durationHours * 60 * 60 * 1000) : end;

  const filled = players.filter((p): p is FilledSlot => p !== null);
  const combinedHandicap = filled.reduce((sum, p) => sum + p.handicap, 0);
  const totalPrice = (resource.pricePerPlayer ?? 0) * filled.length;
  const canConfirm = filled.length > 0 && combinedHandicap <= MAX_COMBINED_HANDICAP && !isSubmitting;

  const selectPlayer = (index: number, selected: UserSearchResult) => {
    setPlayers((prev) => {
      const next = [...prev];
      next[index] = { userId: selected.id, name: selected.name, handicap: selected.handicap, paymentMethod: 'Cash' };
      return next;
    });
    setSearchingIndex(null);
  };

  const removePlayer = (index: number) => {
    setPlayers((prev) => {
      const next = [...prev];
      next[index] = null;
      return next;
    });
  };

  const setPaymentMethod = (index: number, paymentMethod: PaymentMethod) => {
    setPlayers((prev) => {
      const next = [...prev];
      const player = next[index];
      if (player) next[index] = { ...player, paymentMethod };
      return next;
    });
  };

  const handleConfirm = async () => {
    setError(null);
    setIsSubmitting(true);
    try {
      const request: CreateBookingRequest = {
        resourceId: resource.id,
        start: toIso(start),
        end: toIso(bookingEnd),
        players: filled.map((p) => ({ userId: p.userId, paymentMethod: p.paymentMethod })),
      };
      await api.post('/api/admin/bookings', request);
      onChanged();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not create the booking.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal title={`${resource.name} · ${formatTime(start.toISOString())}`} onClose={onClose}>
      <div className="booking-dialog-slots">
        {players.map((player, index) =>
          player ? (
            <FilledPlayerRow
              key={index}
              player={player}
              onRemove={() => removePlayer(index)}
              onPaymentMethodChange={(pm) => setPaymentMethod(index, pm)}
            />
          ) : searchingIndex === index ? (
            <PlayerSearch
              key={index}
              excludeUserIds={filled.map((p) => p.userId)}
              onSelect={(u) => selectPlayer(index, u)}
              onCancel={() => setSearchingIndex(null)}
            />
          ) : (
            <button
              key={index}
              type="button"
              className="add-player-slot"
              onClick={() => setSearchingIndex(index)}
            >
              + Add player
            </button>
          ),
        )}
      </div>

      {isSimulator && (
        <label className="duration-option">
          Session length
          <select value={durationHours} onChange={(e) => setDurationHours(Number(e.target.value))}>
            {Array.from({ length: MAX_SIMULATOR_HOURS - MIN_SIMULATOR_HOURS + 1 }, (_, i) => MIN_SIMULATOR_HOURS + i).map(
              (hours) => (
                <option key={hours} value={hours}>
                  {hours} hour{hours > 1 ? 's' : ''}
                </option>
              ),
            )}
          </select>
        </label>
      )}

      <div className="booking-dialog-footer">
        <div className="booking-dialog-summary">
          <span className={combinedHandicap > MAX_COMBINED_HANDICAP ? 'handicap-over' : undefined}>
            Handicap {combinedHandicap}/{MAX_COMBINED_HANDICAP}
          </span>
          <span>Total €{totalPrice.toFixed(2)}</span>
        </div>
        {error && <p className="modal-error">{error}</p>}
        <button type="button" className="modal-submit" disabled={!canConfirm} onClick={handleConfirm}>
          {isSubmitting ? 'Booking…' : 'Confirm booking'}
        </button>
      </div>
    </Modal>
  );
}

function ViewDialog({
  resource,
  booking,
  onClose,
  onChanged,
}: {
  resource: Resource;
  booking: Booking;
  onClose: () => void;
  onChanged: () => void;
}) {
  const [error, setError] = useState<string | null>(null);
  const [isCancelling, setIsCancelling] = useState(false);
  const [isMarkingPaid, setIsMarkingPaid] = useState(false);

  const handleCancel = async () => {
    setError(null);
    setIsCancelling(true);
    try {
      await api.delete(`/api/admin/bookings/${booking.id}`);
      onChanged();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not cancel this booking.');
    } finally {
      setIsCancelling(false);
    }
  };

  const handleMarkPaid = async () => {
    setError(null);
    setIsMarkingPaid(true);
    try {
      await api.post(`/api/admin/bookings/${booking.id}/mark-paid`);
      onChanged();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not mark this booking as paid.');
    } finally {
      setIsMarkingPaid(false);
    }
  };

  return (
    <Modal title={`${resource.name} · ${formatTime(booking.start)}`} onClose={onClose}>
      <div className="booking-dialog-slots">
        {booking.players.map((player) => (
          <div key={player.userId} className="filled-player-row locked">
            <div className="filled-player-info">
              <span className="filled-player-name">
                {player.name}
                {player.userId === booking.bookerId && ' (booker)'}
              </span>
              <span className="filled-player-handicap">
                hcp {player.handicap} · {player.paymentMethod}
              </span>
            </div>
          </div>
        ))}
      </div>

      <div className="booking-dialog-footer">
        <div className="booking-dialog-summary">
          <span>
            Handicap {booking.combinedHandicap}/{MAX_COMBINED_HANDICAP}
          </span>
          <span>Total €{booking.totalPrice.toFixed(2)}</span>
        </div>
        <div className="booking-dialog-summary">
          <span>{booking.isPaid ? 'Paid' : 'Not paid'}</span>
          <span>{booking.status}</span>
        </div>
        <p className="booking-dialog-cart">Cart: {booking.cartName ?? 'None'}</p>
        {error && <p className="modal-error">{error}</p>}
        <div className="booking-dialog-actions">
          {!booking.isPaid && (
            <button type="button" className="booking-mark-paid" disabled={isMarkingPaid} onClick={handleMarkPaid}>
              {isMarkingPaid ? 'Marking…' : 'Mark as paid'}
            </button>
          )}
          <button type="button" className="booking-cancel" disabled={isCancelling} onClick={handleCancel}>
            {isCancelling ? 'Cancelling…' : 'Cancel booking'}
          </button>
        </div>
      </div>
    </Modal>
  );
}

function FilledPlayerRow({
  player,
  onRemove,
  onPaymentMethodChange,
}: {
  player: FilledSlot;
  onRemove: () => void;
  onPaymentMethodChange: (paymentMethod: PaymentMethod) => void;
}) {
  return (
    <div className="filled-player-row">
      <div className="filled-player-info">
        <span className="filled-player-name">{player.name}</span>
        <span className="filled-player-handicap">hcp {player.handicap}</span>
      </div>
      <select
        value={player.paymentMethod}
        onChange={(e) => onPaymentMethodChange(e.target.value as PaymentMethod)}
      >
        <option value="Cash">Cash</option>
        <option value="Card">Card</option>
      </select>
      <button type="button" className="remove-player" onClick={onRemove} aria-label="Remove player">
        ×
      </button>
    </div>
  );
}

function toIso(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}T${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:00`;
}

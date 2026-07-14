import { useEffect, useState } from 'react';
import { Modal } from './Modal';
import { PlayerSearch } from './PlayerSearch';
import { api, ApiError } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { CART_PRICE, MAX_COMBINED_HANDICAP, MAX_PLAYERS } from '../constants';
import type { CartAvailability, CreateBookingRequest, PaymentMethod, Resource, TimeSlot, UserSearchResult } from '../api/types';
import { formatTime, toNaiveIso } from '../utils/date';
import './BookingDialog.css';

const MIN_SIMULATOR_HOURS = 1;
const MAX_SIMULATOR_HOURS = 5;

type FilledSlot = { userId: string; name: string; handicap: number; paymentMethod: PaymentMethod };

interface BookingDialogProps {
  resource: Resource;
  slot: TimeSlot;
  onClose: () => void;
  onBooked: () => void;
}

export function BookingDialog({ resource, slot, onClose, onBooked }: BookingDialogProps) {
  const { user } = useAuth();
  const isJoin = !slot.isAvailable;

  return isJoin ? (
    <JoinDialog resource={resource} slot={slot} onClose={onClose} onBooked={onBooked} />
  ) : (
    <CreateDialog resource={resource} slot={slot} currentUser={user!} onClose={onClose} onBooked={onBooked} />
  );
}

function DialogFooter({
  combinedHandicap,
  totalPrice,
  confirmLabel,
  canConfirm,
  isSubmitting,
  error,
  onConfirm,
}: {
  combinedHandicap: number;
  totalPrice: number;
  confirmLabel: string;
  canConfirm: boolean;
  isSubmitting: boolean;
  error: string | null;
  onConfirm: () => void;
}) {
  const isOverCap = combinedHandicap > MAX_COMBINED_HANDICAP;

  return (
    <div className="booking-dialog-footer">
      <div className="booking-dialog-summary">
        <span className={isOverCap ? 'handicap-over' : undefined}>
          Handicap {combinedHandicap}/{MAX_COMBINED_HANDICAP}
        </span>
        <span>Total €{totalPrice.toFixed(2)}</span>
      </div>
      {error && <p className="modal-error">{error}</p>}
      <button type="button" className="modal-submit" disabled={!canConfirm} onClick={onConfirm}>
        {isSubmitting ? 'Booking…' : confirmLabel}
      </button>
    </div>
  );
}

function CreateDialog({
  resource,
  slot,
  currentUser,
  onClose,
  onBooked,
}: {
  resource: Resource;
  slot: TimeSlot;
  currentUser: { id: string; name: string; handicap: number };
  onClose: () => void;
  onBooked: () => void;
}) {
  const [players, setPlayers] = useState<(FilledSlot | null)[]>(() => {
    const initial: (FilledSlot | null)[] = [
      { userId: currentUser.id, name: currentUser.name, handicap: currentUser.handicap, paymentMethod: 'Cash' },
    ];
    while (initial.length < MAX_PLAYERS) initial.push(null);
    return initial;
  });
  const [searchingIndex, setSearchingIndex] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [wantsCart, setWantsCart] = useState(false);
  const [isCartAvailable, setIsCartAvailable] = useState<boolean | null>(null);
  const [durationHours, setDurationHours] = useState(MIN_SIMULATOR_HOURS);

  const isSimulator = resource.type === 'Simulator';
  const bookingEnd = isSimulator
    ? toNaiveIso(new Date(new Date(slot.start).getTime() + durationHours * 60 * 60 * 1000))
    : slot.end;

  useEffect(() => {
    if (isSimulator) return; // no cart option for simulators — see below
    api
      .get<CartAvailability>(`/api/carts/availability?start=${encodeURIComponent(slot.start)}`)
      .then((res) => setIsCartAvailable(res.isAvailable))
      .catch(() => setIsCartAvailable(false));
  }, [slot.start, isSimulator]);

  const filled = players.filter((p): p is FilledSlot => p !== null);
  const combinedHandicap = filled.reduce((sum, p) => sum + p.handicap, 0);
  const totalPrice = (resource.pricePerPlayer ?? 0) * filled.length + (wantsCart ? CART_PRICE : 0);
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
        start: slot.start,
        end: bookingEnd,
        players: filled.map((p) => ({ userId: p.userId, paymentMethod: p.paymentMethod })),
        wantsCart: isSimulator ? false : wantsCart,
      };
      await api.post('/api/bookings', request);
      onBooked();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not create the booking.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal title={`${resource.name} · ${formatTime(slot.start)}`} onClose={onClose}>
      <div className="booking-dialog-slots">
        {players.map((player, index) =>
          player ? (
            <FilledPlayerRow
              key={index}
              player={player}
              canRemove={index !== 0}
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

      {!isSimulator && (
        <label className={'cart-option' + (isCartAvailable === false ? ' cart-option-disabled' : '')}>
          <input
            type="checkbox"
            checked={wantsCart}
            disabled={isCartAvailable !== true}
            onChange={(e) => setWantsCart(e.target.checked)}
          />
          {isCartAvailable === false ? 'No carts available' : `Add a golf cart (+€${CART_PRICE.toFixed(2)})`}
        </label>
      )}

      <DialogFooter
        combinedHandicap={combinedHandicap}
        totalPrice={totalPrice}
        confirmLabel="Confirm booking"
        canConfirm={canConfirm}
        isSubmitting={isSubmitting}
        error={error}
        onConfirm={handleConfirm}
      />
    </Modal>
  );
}

function JoinDialog({
  resource,
  slot,
  onClose,
  onBooked,
}: {
  resource: Resource;
  slot: TimeSlot;
  onClose: () => void;
  onBooked: () => void;
}) {
  const { user } = useAuth();
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('Cash');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const existingCount = slot.playerCount ?? 0;
  const existingHandicap = slot.combinedHandicap ?? 0;
  const combinedHandicap = existingHandicap + user!.handicap;
  const canConfirm = combinedHandicap <= MAX_COMBINED_HANDICAP && !isSubmitting;

  const handleConfirm = async () => {
    setError(null);
    setIsSubmitting(true);
    try {
      await api.post(`/api/bookings/${slot.bookingId}/players`, { userId: user!.id, paymentMethod });
      onBooked();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not join this booking.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal title={`Join ${resource.name} · ${formatTime(slot.start)}`} onClose={onClose}>
      <div className="booking-dialog-slots">
        {Array.from({ length: existingCount }, (_, i) => (
          <div key={`booked-${i}`} className="filled-player-row locked">
            <span>Player already booked</span>
          </div>
        ))}
        <FilledPlayerRow
          player={{ userId: user!.id, name: user!.name, handicap: user!.handicap, paymentMethod }}
          canRemove={false}
          onRemove={() => {}}
          onPaymentMethodChange={setPaymentMethod}
        />
        {Array.from({ length: MAX_PLAYERS - existingCount - 1 }, (_, i) => (
          <div key={`open-${i}`} className="add-player-slot disabled">
            Open
          </div>
        ))}
      </div>

      <DialogFooter
        combinedHandicap={combinedHandicap}
        totalPrice={resource.pricePerPlayer ?? 0}
        confirmLabel="Join booking"
        canConfirm={canConfirm}
        isSubmitting={isSubmitting}
        error={error}
        onConfirm={handleConfirm}
      />
    </Modal>
  );
}

function FilledPlayerRow({
  player,
  canRemove,
  onRemove,
  onPaymentMethodChange,
}: {
  player: FilledSlot;
  canRemove: boolean;
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
      {canRemove && (
        <button type="button" className="remove-player" onClick={onRemove} aria-label="Remove player">
          ×
        </button>
      )}
    </div>
  );
}

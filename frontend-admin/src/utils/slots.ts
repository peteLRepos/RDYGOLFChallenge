import type { Booking, Resource } from '../api/types';

export interface AdminSlot {
  start: Date;
  end: Date;
  booking: Booking | null;
}

/**
 * Mirrors the backend's own slot generation (BookingService.GetAvailabilityAsync) client-side,
 * so the admin tee sheet can be built from a single GET /api/admin/bookings call (which already
 * carries payment status, players, everything) instead of a second endpoint.
 */
export function buildSlotsForDate(resource: Resource, date: Date, bookings: Booking[]): AdminSlot[] {
  const [openHour, openMinute] = resource.openingTime.split(':').map(Number);
  const [closeHour, closeMinute] = resource.closingTime.split(':').map(Number);
  const dayStart = new Date(date.getFullYear(), date.getMonth(), date.getDate(), openHour, openMinute);
  const dayEnd = new Date(date.getFullYear(), date.getMonth(), date.getDate(), closeHour, closeMinute);
  const durationMs = resource.slotDurationMinutes * 60_000;

  // A cancelled booking frees its slot back up, same as the public availability endpoint.
  const activeBookings = bookings.filter((b) => b.resourceId === resource.id && b.status !== 'Cancelled');

  const slots: AdminSlot[] = [];
  for (let start = dayStart; start.getTime() + durationMs <= dayEnd.getTime(); start = new Date(start.getTime() + durationMs)) {
    const end = new Date(start.getTime() + durationMs);
    const booking = activeBookings.find((b) => {
      const bStart = new Date(b.start).getTime();
      const bEnd = new Date(b.end).getTime();
      return start.getTime() < bEnd && bStart < end.getTime();
    });
    slots.push({ start, end, booking: booking ?? null });
  }
  return slots;
}

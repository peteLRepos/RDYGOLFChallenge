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

// One row per hour, however many sub-hourly slots fall in it — a 10-minute-slot course gets six
// slots per row, a 60-minute one gets exactly one, no special-casing needed either way.
export function groupSlotsByHour(slots: AdminSlot[]): { hour: number; slots: AdminSlot[] }[] {
  const groups: { hour: number; slots: AdminSlot[] }[] = [];
  for (const slot of slots) {
    const hour = slot.start.getHours();
    const current = groups.at(-1);
    if (current?.hour === hour) {
      current.slots.push(slot);
    } else {
      groups.push({ hour, slots: [slot] });
    }
  }
  return groups;
}

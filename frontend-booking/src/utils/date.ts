export function startOfToday(): Date {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
}

export function addDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

export function isSameDay(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

/** yyyy-MM-dd, matching the API's DateOnly query parameter format. */
export function toDateKey(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function formatDayLabel(date: Date): string {
  return date.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
}

export function formatTime(isoString: string): string {
  return new Date(isoString).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
}

/** Hour-only label for a row of sub-hourly slots, e.g. "7 AM" — see TeeSheetPage's hour grouping. */
export function formatHourLabel(isoString: string): string {
  return new Date(isoString).toLocaleTimeString(undefined, { hour: 'numeric' });
}

/** yyyy-MM-ddTHH, a slot's hour bucket key — groups sub-hourly slots (e.g. 10-minute tee times)
 * into one row per hour on the tee sheet. Safe as a plain string prefix since slot.start is always
 * a naive local timestamp already in that exact format (see toNaiveIso). */
export function hourKey(isoString: string): string {
  return isoString.slice(0, 13);
}

/** yyyy-MM-ddTHH:mm:ss, matching the API's naive-local-timestamp format (no UTC conversion — see
 * README's timezone assumption). Used when computing a new time client-side (e.g. a simulator
 * booking's end), never via Date.toISOString(), which would shift by the browser's UTC offset. */
export function toNaiveIso(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
}

export function formatDateTime(isoString: string): string {
  return new Date(isoString).toLocaleString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

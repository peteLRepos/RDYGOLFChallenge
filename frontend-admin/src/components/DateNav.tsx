import { addDays, formatDayLabel, isSameDay, startOfToday } from '../utils/date';
import './DateNav.css';

interface DateNavProps {
  date: Date;
  onChange: (date: Date) => void;
}

export function DateNav({ date, onChange }: DateNavProps) {
  const isToday = isSameDay(date, startOfToday());

  return (
    <div className="date-nav">
      <button
        type="button"
        className="date-nav-button"
        onClick={() => onChange(addDays(date, -1))}
        aria-label="Previous day"
      >
        ‹
      </button>
      <span className="date-nav-label">{isToday ? 'Today' : formatDayLabel(date)}</span>
      <button
        type="button"
        className="date-nav-button"
        onClick={() => onChange(addDays(date, 1))}
        aria-label="Next day"
      >
        ›
      </button>
    </div>
  );
}

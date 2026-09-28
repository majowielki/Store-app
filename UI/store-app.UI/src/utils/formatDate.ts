// Dates are written the same way everywhere in the shop, in English, whatever language the
// visitor's browser is set to; the time of day is the visitor's own.
const dateFormat = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
const dateTimeFormat = new Intl.DateTimeFormat('en-US', {
  month: 'short',
  day: 'numeric',
  year: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
});

const toDate = (value: string | Date): Date | null => {
  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
};

/** "Sep 24, 2026"; an unreadable value gives an empty string. */
export const formatDate = (value: string | Date): string => {
  const date = toDate(value);
  return date ? dateFormat.format(date) : '';
};

/** "Sep 24, 2026, 9:55 PM"; an unreadable value gives an empty string. */
export const formatDateTime = (value: string | Date): string => {
  const date = toDate(value);
  return date ? dateTimeFormat.format(date) : '';
};

const dayFormat = new Intl.DateTimeFormat('en-US', { weekday: 'short', month: 'short', day: 'numeric' });

/** A calendar date from the API ("2026-09-30") as that day in the visitor's zone, not UTC midnight. */
const toDay = (value: string): Date | null => {
  const [year, month, day] = value.split('-').map(Number);
  return year && month && day ? new Date(year, month - 1, day) : null;
};

/** "Wed, Sep 30 – Fri, Oct 2" for a delivery window, "Wed, Sep 30" when it is one day. */
export const formatDayRange = (from: string, to: string): string => {
  const first = toDay(from);
  const last = toDay(to);
  if (!first || !last) return '';
  return from === to ? dayFormat.format(first) : `${dayFormat.format(first)} – ${dayFormat.format(last)}`;
};

const MS_PER_DAY = 24 * 60 * 60 * 1000;

/** Whole days from today to a calendar date from the API ("2026-09-30"), in the visitor's zone; null for an unreadable value. */
export const daysFromToday = (value: string, today: Date = new Date()): number | null => {
  const day = toDay(value);
  if (!day) return null;
  const midnight = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  // Rounded: a day across a daylight saving change is an hour shorter or longer
  return Math.round((day.getTime() - midnight.getTime()) / MS_PER_DAY);
};

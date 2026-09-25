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

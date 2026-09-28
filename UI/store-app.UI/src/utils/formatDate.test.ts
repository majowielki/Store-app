import { describe, expect, it } from 'vitest';
import { daysFromToday, formatDate, formatDateTime, formatDayRange } from './formatDate';

// Newer ICU puts a narrow no-break space before AM/PM; the tests read any space as a space
const plain = (text: string) => text.replace(/\s/g, ' ');

describe('formatDate', () => {
  it('writes the date in English, month first', () => {
    expect(formatDate(new Date(2026, 8, 24, 21, 55))).toBe('Sep 24, 2026');
  });

  it('reads the ISO strings the API sends', () => {
    const local = new Date(2026, 0, 5, 12, 0);
    expect(formatDate(local.toISOString())).toBe('Jan 5, 2026');
  });

  it('gives an empty string for a value that is not a date', () => {
    expect(formatDate('not a date')).toBe('');
  });
});

describe('formatDateTime', () => {
  it('adds the time of day on a 12-hour clock', () => {
    expect(plain(formatDateTime(new Date(2026, 8, 24, 21, 55)))).toBe('Sep 24, 2026, 9:55 PM');
  });

  it('gives an empty string for a value that is not a date', () => {
    expect(formatDateTime('')).toBe('');
  });
});

describe('formatDayRange', () => {
  it('writes a delivery window with the days of the week', () => {
    expect(formatDayRange('2026-09-30', '2026-10-02')).toBe('Wed, Sep 30 – Fri, Oct 2');
  });

  it('reads the dates as calendar days, not as UTC midnight', () => {
    // A zone west of UTC would turn midnight UTC into the day before
    expect(formatDayRange('2026-10-05', '2026-10-05')).toBe('Mon, Oct 5');
  });

  it('gives an empty string for a value that is not a date', () => {
    expect(formatDayRange('soon', '2026-10-05')).toBe('');
  });
});

describe('daysFromToday', () => {
  const today = new Date(2026, 8, 28, 23, 30);

  it('counts calendar days whatever the time of day', () => {
    expect(daysFromToday('2026-09-28', today)).toBe(0);
    expect(daysFromToday('2026-10-02', today)).toBe(4);
  });

  it('counts across a daylight saving change as whole days', () => {
    expect(daysFromToday('2026-11-02', new Date(2026, 9, 24, 8, 0))).toBe(9);
  });

  it('gives null for a value that is not a date', () => {
    expect(daysFromToday('soon', today)).toBeNull();
  });
});

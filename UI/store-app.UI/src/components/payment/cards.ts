import type { ChargeResult } from '@/api/types';

/** The most digits a card number has, and how many are printed in a group. */
export const CARD_NUMBER_MAX_DIGITS = 19;
const CARD_DIGIT_GROUP = 4;

/** "MM / YY": two digits of the month and two of the year. */
const EXPIRY_DIGITS = 4;
const EXPIRY_PART_DIGITS = 2;
const CENTURY = 2000;

/** A security code has three or four digits. */
export const CVC_MAX_DIGITS = 4;

/** What the "Use" button of a test card fills in: any code works, and a date a few years ahead. */
export const SAMPLE_CVC = '123';
const SAMPLE_EXPIRY_YEARS_AHEAD = 3;
const SAMPLE_EXPIRY_MONTH = 12;

/** What each test card does, as the list next to the form says it. */
export const chargeOutcomes: Record<ChargeResult, string> = {
  approved: 'Pays',
  authenticationRequired: 'Asks for 3-D Secure',
  insufficientFunds: 'No funds',
  declined: 'Declined',
};

/** What to tell the customer when a card is refused, by the reason the payment service gives. */
export const declineMessages: Record<string, string> = {
  'card-declined': 'Your card was declined. Try another card.',
  'insufficient-funds': 'There is not enough money on this card. Try another one.',
  'authentication-failed': 'Your bank could not confirm it was you. Try again, or use another card.',
};

const digitsOf = (value: string) => value.replace(/\D/g, '');

/** Digits in groups of four, the way they are printed on a card; at most 19 digits. */
export const formatCardNumber = (value: string): string =>
  digitsOf(value)
    .slice(0, CARD_NUMBER_MAX_DIGITS)
    .replace(new RegExp(`(\\d{${CARD_DIGIT_GROUP}})(?=\\d)`, 'g'), '$1 ');

/** "MM / YY" while typing: the slash appears once the month is complete. */
export const formatExpiry = (value: string): string => {
  const digits = digitsOf(value).slice(0, EXPIRY_DIGITS);
  return digits.length > EXPIRY_PART_DIGITS ? `${digits.slice(0, EXPIRY_PART_DIGITS)} / ${digits.slice(EXPIRY_PART_DIGITS)}` : digits;
};

/** The month and the four-digit year of "MM / YY"; null until both are there. */
export const parseExpiry = (value: string): { month: number; year: number } | null => {
  const digits = digitsOf(value);
  if (digits.length !== EXPIRY_DIGITS) return null;
  return { month: Number(digits.slice(0, EXPIRY_PART_DIGITS)), year: CENTURY + Number(digits.slice(EXPIRY_PART_DIGITS)) };
};

/** Only the digits of a security code, at most four. */
export const formatCvc = (value: string): string => digitsOf(value).slice(0, CVC_MAX_DIGITS);

/** A date a test card is still valid at: December, a few years ahead. */
export const sampleExpiry = (now = new Date()): string =>
  `${SAMPLE_EXPIRY_MONTH} / ${String((now.getFullYear() + SAMPLE_EXPIRY_YEARS_AHEAD) % 100).padStart(EXPIRY_PART_DIGITS, '0')}`;

/** "Visa •••• 4242". */
export const describeCard = (brand?: string | null, last4?: string | null): string =>
  brand && last4 ? `${brand.charAt(0).toUpperCase()}${brand.slice(1)} •••• ${last4}` : 'Your card';

/** The test cards the demo takes, with what each one does (numbered like Stripe's test mode). */
export const testCards = [
  { number: '4242 4242 4242 4242', outcome: 'Pays' },
  { number: '4000 0000 0000 3220', outcome: 'Asks for 3-D Secure' },
  { number: '4000 0000 0000 9995', outcome: 'No funds' },
  { number: '4000 0000 0000 0002', outcome: 'Declined' },
] as const;

/** What to tell the customer when a card is refused. */
export const declineMessages: Record<string, string> = {
  'card-declined': 'Your card was declined. Try another card.',
  'insufficient-funds': 'There is not enough money on this card. Try another one.',
  'authentication-failed': 'Your bank could not confirm it was you. Try again, or use another card.',
};

/** Digits in groups of four, the way they are printed on a card; at most 19 digits. */
export const formatCardNumber = (value: string): string =>
  value
    .replace(/\D/g, '')
    .slice(0, 19)
    .replace(/(\d{4})(?=\d)/g, '$1 ');

/** "MM / YY" while typing: the slash appears once the month is complete. */
export const formatExpiry = (value: string): string => {
  const digits = value.replace(/\D/g, '').slice(0, 4);
  return digits.length > 2 ? `${digits.slice(0, 2)} / ${digits.slice(2)}` : digits;
};

/** The month and the four-digit year of "MM / YY"; null until both are there. */
export const parseExpiry = (value: string): { month: number; year: number } | null => {
  const digits = value.replace(/\D/g, '');
  if (digits.length !== 4) return null;
  return { month: Number(digits.slice(0, 2)), year: 2000 + Number(digits.slice(2)) };
};

/** A date a test card is still valid at: December, three years ahead. */
export const sampleExpiry = (now = new Date()): string => `12 / ${String((now.getFullYear() + 3) % 100).padStart(2, '0')}`;

/** "Visa •••• 4242". */
export const describeCard = (brand?: string | null, last4?: string | null): string =>
  brand && last4 ? `${brand.charAt(0).toUpperCase()}${brand.slice(1)} •••• ${last4}` : 'Your card';

import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { Order, OrderPayment, Payment } from '@/api/types';
import { order, user } from '@/test/fixtures';
import { api, json, problemResponse } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import PayOrder from './PayOrder';

const PAYMENT_ID = '6f1d2c3b-4a59-4e6f-8a7b-9c0d1e2f3a4b';
// Polling asks for the order every second
const patient = { timeout: 4000 };

const inFifteenMinutes = () => new Date(Date.now() + 15 * 60_000).toISOString();

const awaiting = (): Order => order({ status: 'AwaitingPayment', paymentDueAt: inFifteenMinutes() });
const paid = (): Order => order({ status: 'Paid', cardBrand: 'visa', cardLast4: '4242' });

const opened: OrderPayment = { orderId: 100, paymentId: PAYMENT_ID, amount: 320, currency: 'usd', status: 'requiresPaymentMethod', paymentDueAt: inFifteenMinutes() };

const payment = (overrides: Partial<Payment>): Payment => ({
  id: PAYMENT_ID,
  orderId: 100,
  amount: 320,
  currency: 'usd',
  status: 'succeeded',
  createdAt: '2026-01-02T00:00:00Z',
  ...overrides,
});

/** The order as the server has it; the handlers read it on every poll. */
let current: Order;
let confirmed: { cardNumber: string; expMonth: number; expYear: number; cvc: string }[];

const answerForCard = (cardNumber: string): Payment => {
  const digits = cardNumber.replace(/\D/g, '');
  if (digits === '4000000000003220') return payment({ status: 'requiresAction', cardBrand: 'visa', cardLast4: '3220' });
  if (digits === '4000000000000002') return payment({ status: 'requiresPaymentMethod', declineReason: 'card-declined', cardBrand: 'visa', cardLast4: '0002' });
  // The webhook reaches the shop a moment after the card went through
  window.setTimeout(() => (current = paid()), 300);
  return payment({ status: 'succeeded', cardBrand: 'visa', cardLast4: '4242' });
};

const renderPage = () => renderWithStore(<PayOrder />, { route: '/orders/100/pay', path: '/orders/:id/pay', user });

beforeEach(() => {
  current = awaiting();
  confirmed = [];
  server.use(
    http.get(api('/orders/100'), () => json(current)),
    http.post(api('/orders/100/payment'), () => json(opened)),
    http.post(api(`/payments/${PAYMENT_ID}/confirm`), async ({ request }) => {
      const card = (await request.json()) as (typeof confirmed)[number];
      confirmed.push(card);
      return json(answerForCard(card.cardNumber));
    }),
    http.post(api(`/payments/${PAYMENT_ID}/authenticate`), async ({ request }) => {
      const { approve } = (await request.json()) as { approve: boolean };
      if (!approve) return json(payment({ status: 'requiresPaymentMethod', declineReason: 'authentication-failed' }));
      window.setTimeout(() => (current = paid()), 300);
      return json(payment({ status: 'succeeded' }));
    }),
  );
});

const fillCard = async (number: string) => {
  const person = userEvent.setup();
  await person.clear(screen.getByLabelText('Card number'));
  await person.type(screen.getByLabelText('Card number'), number);
  await person.clear(screen.getByLabelText('Expiry (MM / YY)'));
  await person.type(screen.getByLabelText('Expiry (MM / YY)'), '1230');
  await person.clear(screen.getByLabelText('Security code'));
  await person.type(screen.getByLabelText('Security code'), '123');
  await person.click(screen.getByRole('button', { name: /^Pay \$320\.00$/ }));
};

describe('PayOrder', () => {
  it('takes a card, waits for the shop to hear of it and thanks the customer', async () => {
    renderPage();

    expect(await screen.findByRole('timer')).toHaveTextContent(/Your pieces are held for 1[45]:\d\d/);
    await fillCard('4242424242424242');

    expect(await screen.findByRole('heading', { name: 'Confirming your payment' })).toBeInTheDocument();
    expect(await screen.findByText('Payment received', {}, patient)).toBeInTheDocument();
    expect(screen.getByText(/\$320\.00 paid with Visa •••• 4242/)).toBeInTheDocument();
    expect(confirmed).toEqual([{ cardNumber: '4242 4242 4242 4242', expMonth: 12, expYear: 2030, cvc: '123' }]);
  });

  it('shows a refused card next to the form and takes another one', async () => {
    renderPage();
    await screen.findByRole('form', { name: 'Card details' });

    await fillCard('4000000000000002');
    expect(await screen.findByRole('alert')).toHaveTextContent('Your card was declined. Try another card.');

    await fillCard('4242424242424242');
    expect(await screen.findByText('Payment received', {}, patient)).toBeInTheDocument();
  });

  it('asks for the 3-D Secure approval of a card that needs it', async () => {
    renderPage();
    await screen.findByRole('form', { name: 'Card details' });

    await fillCard('4000000000003220');
    const bank = await screen.findByRole('dialog', { name: 'Is this you?' });
    expect(bank).toHaveTextContent('$320.00');
    expect(bank).toHaveTextContent('Visa •••• 3220');
    await userEvent.click(within(bank).getByRole('button', { name: 'Approve' }));

    expect(await screen.findByText('Payment received', {}, patient)).toBeInTheDocument();
  });

  it('says so when the customer rejects the 3-D Secure check', async () => {
    renderPage();
    await screen.findByRole('form', { name: 'Card details' });

    await fillCard('4000000000003220');
    await userEvent.click(within(await screen.findByRole('dialog', { name: 'Is this you?' })).getByRole('button', { name: 'Reject' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Your bank could not confirm it was you.');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('fills the form with a test card', async () => {
    renderPage();
    await screen.findByRole('form', { name: 'Card details' });

    await userEvent.click(screen.getByRole('button', { name: 'Use test card 4000 0000 0000 9995' }));

    expect(screen.getByLabelText('Card number')).toHaveValue('4000 0000 0000 9995');
    expect((screen.getByLabelText('Expiry (MM / YY)') as HTMLInputElement).value).toMatch(/^12 \/ \d\d$/);
    expect(screen.getByLabelText('Security code')).toHaveValue('123');
  });

  it('waits while the pieces are being reserved', async () => {
    current = order({ status: 'Placed' });
    window.setTimeout(() => (current = awaiting()), 500);
    renderPage();

    expect(await screen.findByRole('heading', { name: 'Reserving your pieces' })).toBeInTheDocument();
    expect(await screen.findByRole('form', { name: 'Card details' }, patient)).toBeInTheDocument();
  });

  it('explains an order cancelled because a piece sold out', async () => {
    current = order({ status: 'Cancelled', cancellationReason: 'out-of-stock' });
    renderPage();

    expect(await screen.findByRole('heading', { name: 'This order was cancelled' })).toBeInTheDocument();
    expect(screen.getByText(/sold out by the time it reached our warehouse/)).toBeInTheDocument();
  });

  it('reads the order again when the payment can no longer be made', async () => {
    server.use(http.post(api(`/payments/${PAYMENT_ID}/confirm`), () => {
      current = order({ status: 'Cancelled', cancellationReason: 'payment-timed-out' });
      return problemResponse(409, 'The order was cancelled, so it can no longer be paid.');
    }));
    renderPage();
    await screen.findByRole('form', { name: 'Card details' });

    await fillCard('4242424242424242');

    expect(await screen.findByRole('heading', { name: 'This order was cancelled' }, patient)).toBeInTheDocument();
  });
});

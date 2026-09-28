import { screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { order } from '@/test/fixtures';
import { renderWithStore } from '@/test/render';
import OrderTimeline, { OrderStatusBadge } from './OrderTimeline';

const steps = () => within(screen.getByRole('list', { name: 'Order progress' })).getAllByRole('listitem');

describe('OrderTimeline', () => {
  it('shows the whole way with the steps ahead still open and the delivery window', () => {
    renderWithStore(<OrderTimeline order={order()} />);

    expect(steps().map((step) => step.textContent)).toEqual([
      expect.stringMatching(/^Order placed.*2026/),
      'Awaiting paymentReserving your pieces',
      'Payment receivedNot yet',
      'ShippedNot yet',
    ]);
    expect(steps()[0]).toHaveAttribute('aria-current', 'step');
    expect(screen.getByText('Tue, Jan 6 – Thu, Jan 8')).toBeInTheDocument();
  });

  it('gives the payment deadline while the order waits for its payment', () => {
    const waiting = order({
      status: 'AwaitingPayment',
      paymentDueAt: '2026-01-02T00:15:00Z',
      statusHistory: [
        { status: 'Placed', changedAt: '2026-01-02T00:00:00Z' },
        { status: 'AwaitingPayment', changedAt: '2026-01-02T00:00:02Z' },
      ],
    });
    renderWithStore(<OrderTimeline order={waiting} />);

    expect(steps()[1]).toHaveAttribute('aria-current', 'step');
    expect(steps()[1]).toHaveTextContent(/^Awaiting paymentPay by Jan 2, 2026/);
  });

  it('dates every step an order has reached and names the card that paid', () => {
    const shipped = order({
      status: 'Shipped',
      cardBrand: 'visa',
      cardLast4: '4242',
      statusHistory: [
        { status: 'Placed', changedAt: '2026-01-02T09:00:00Z' },
        { status: 'AwaitingPayment', changedAt: '2026-01-02T09:00:01Z' },
        { status: 'Paid', changedAt: '2026-01-02T10:00:00Z' },
        { status: 'Shipped', changedAt: '2026-01-03T10:00:00Z' },
      ],
      nextStatuses: [],
    });
    renderWithStore(<OrderTimeline order={shipped} />);

    expect(steps().every((step) => !step.textContent?.includes('Not yet'))).toBe(true);
    expect(steps()[2]).toHaveTextContent('Visa •••• 4242');
    expect(steps()[3]).toHaveAttribute('aria-current', 'step');
    expect(screen.getByText(/On its way, arriving/)).toBeInTheDocument();
  });

  it('shows where a cancelled order stopped, why, and no delivery date', () => {
    const cancelled = order({
      status: 'Cancelled',
      cancellationReason: 'payment-timed-out',
      statusHistory: [
        { status: 'Placed', changedAt: '2026-01-02T09:00:00Z' },
        { status: 'AwaitingPayment', changedAt: '2026-01-02T09:00:01Z' },
        { status: 'Cancelled', changedAt: '2026-01-02T09:15:01Z' },
      ],
      nextStatuses: [],
    });
    renderWithStore(<OrderTimeline order={cancelled} />);

    expect(steps().map((step) => step.textContent?.split('Jan')[0])).toEqual(['Order placed', 'Awaiting payment', 'Cancelled']);
    expect(screen.getByText(/The payment did not arrive in time/)).toBeInTheDocument();
    expect(screen.queryByText(/Estimated delivery/)).not.toBeInTheDocument();
  });

  it('tells the customer a refunded order has its money back', () => {
    const refunded = order({
      status: 'Refunded',
      cancellationReason: 'cancelled-by-administrator',
      cardBrand: 'visa',
      cardLast4: '4242',
      statusHistory: [
        { status: 'Placed', changedAt: '2026-01-02T09:00:00Z' },
        { status: 'AwaitingPayment', changedAt: '2026-01-02T09:00:01Z' },
        { status: 'Paid', changedAt: '2026-01-02T09:05:00Z' },
        { status: 'Cancelled', changedAt: '2026-01-02T12:00:00Z' },
        { status: 'Refunded', changedAt: '2026-01-02T12:00:05Z' },
      ],
      nextStatuses: [],
    });
    renderWithStore(<OrderTimeline order={refunded} />);

    expect(steps().at(-1)).toHaveAttribute('aria-current', 'step');
    expect(screen.getByText(/The payment has been returned to your card/)).toBeInTheDocument();
  });

  it('writes the status out in a list', () => {
    renderWithStore(<OrderStatusBadge status="AwaitingPayment" />);

    expect(screen.getByText('Awaiting payment')).toBeInTheDocument();
  });
});

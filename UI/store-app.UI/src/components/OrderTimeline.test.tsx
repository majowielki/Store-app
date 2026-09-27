import { screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { order } from '@/test/fixtures';
import { renderWithStore } from '@/test/render';
import OrderTimeline from './OrderTimeline';

const steps = () => within(screen.getByRole('list', { name: 'Order progress' })).getAllByRole('listitem');

describe('OrderTimeline', () => {
  it('shows the whole way with the steps ahead still open and the delivery window', () => {
    renderWithStore(<OrderTimeline order={order()} />);

    expect(steps().map((step) => step.textContent)).toEqual([
      expect.stringMatching(/^Order placed.*2026/),
      'Payment receivedNot yet',
      'ShippedNot yet',
    ]);
    expect(steps()[0]).toHaveAttribute('aria-current', 'step');
    expect(screen.getByText('Tue, Jan 6 – Thu, Jan 8')).toBeInTheDocument();
  });

  it('dates every step an order has reached', () => {
    const shipped = order({
      status: 'Shipped',
      statusHistory: [
        { status: 'Placed', changedAt: '2026-01-02T09:00:00Z' },
        { status: 'Paid', changedAt: '2026-01-02T10:00:00Z' },
        { status: 'Shipped', changedAt: '2026-01-03T10:00:00Z' },
      ],
      nextStatuses: [],
    });
    renderWithStore(<OrderTimeline order={shipped} />);

    expect(steps().every((step) => !step.textContent?.includes('Not yet'))).toBe(true);
    expect(steps()[2]).toHaveAttribute('aria-current', 'step');
    expect(screen.getByText(/On its way, arriving/)).toBeInTheDocument();
  });

  it('shows where a cancelled order stopped and no delivery date', () => {
    const cancelled = order({
      status: 'Cancelled',
      statusHistory: [
        { status: 'Placed', changedAt: '2026-01-02T09:00:00Z' },
        { status: 'Cancelled', changedAt: '2026-01-02T11:00:00Z' },
      ],
      nextStatuses: [],
    });
    renderWithStore(<OrderTimeline order={cancelled} />);

    expect(steps().map((step) => step.textContent?.split('Jan')[0])).toEqual(['Order placed', 'Cancelled']);
    expect(screen.queryByText(/Estimated delivery/)).not.toBeInTheDocument();
  });
});

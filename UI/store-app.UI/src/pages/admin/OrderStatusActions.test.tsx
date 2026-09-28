import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import { admin, order } from '@/test/fixtures';
import { api, json, problemResponse } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import OrderStatusActions from './OrderStatusActions';

const paid = () => order({ status: 'Paid', nextStatuses: ['Shipped', 'Cancelled'] });

describe('OrderStatusActions', () => {
  it('offers the moves the order allows and sends the one pressed', async () => {
    const sent: unknown[] = [];
    server.use(
      http.patch(api('/admin/orders/100/status'), async ({ request }) => {
        sent.push(await request.json());
        return json(order({ status: 'Shipped', nextStatuses: [] }));
      }),
    );
    renderWithStore(<OrderStatusActions order={paid()} />, { user: admin });

    expect(screen.getAllByRole('button').map((button) => button.textContent)).toEqual(['Mark as shipped', 'Cancel order']);
    await userEvent.click(screen.getByRole('button', { name: 'Mark as shipped' }));

    expect(await screen.findByText('Order #100 is now shipped.', { exact: true })).toBeInTheDocument();
    expect(sent).toEqual([{ status: 'Shipped' }]);
  });

  it('offers only cancelling while the order waits for its payment', () => {
    renderWithStore(<OrderStatusActions order={order({ status: 'AwaitingPayment', nextStatuses: ['Cancelled'] })} />, { user: admin });

    expect(screen.getAllByRole('button', { name: /mark as|cancel/i }).map((button) => button.textContent)).toEqual(['Cancel order']);
  });

  it('asks before cancelling and says a paid order is refunded', async () => {
    const sent: unknown[] = [];
    server.use(
      http.patch(api('/admin/orders/100/status'), async ({ request }) => {
        sent.push(await request.json());
        return json(order({ status: 'Cancelled', nextStatuses: [] }));
      }),
    );
    renderWithStore(<OrderStatusActions order={paid()} />, { user: admin });

    await userEvent.click(screen.getByRole('button', { name: 'Cancel order' }));
    const dialog = await screen.findByRole('dialog', { name: 'Cancel order #100?' });
    expect(dialog).toHaveTextContent('the payment is refunded to their card');
    expect(sent).toEqual([]);
    await userEvent.click(within(dialog).getByRole('button', { name: 'Cancel order' }));

    expect(await screen.findByText('Order #100 is now cancelled.', { exact: true })).toBeInTheDocument();
    expect(sent).toEqual([{ status: 'Cancelled' }]);
  });

  it('shows the refusal and leaves the order as it was', async () => {
    server.use(http.patch(api('/admin/orders/100/status'), () => problemResponse(409, 'The order is already shipped.')));
    renderWithStore(<OrderStatusActions order={paid()} />, { user: admin });

    await userEvent.click(screen.getByRole('button', { name: 'Mark as shipped' }));

    expect(await screen.findByText('The order is already shipped.', { exact: true })).toBeInTheDocument();
  });

  it('has nothing to offer for a shipped order', () => {
    renderWithStore(<OrderStatusActions order={order({ status: 'Shipped', nextStatuses: [] })} />, { user: admin });

    expect(screen.queryByRole('button', { name: /mark as|cancel/i })).not.toBeInTheDocument();
  });
});

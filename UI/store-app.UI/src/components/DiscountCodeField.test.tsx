import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import { user } from '@/test/fixtures';
import { api, json, problemResponse } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import CartTotals from './CartTotals';
import DiscountCodeField from './DiscountCodeField';

// The server cart holds one oak table at 320; the customer has ordered before
const Summary = () => (
  <>
    <DiscountCodeField />
    <CartTotals />
  </>
);

const returningCustomer = () => server.use(http.get(api('/orders/has-orders'), () => json({ hasOrders: true, ordersCount: 1 })));

const oak50 = { code: 'OAK50', kind: 'Amount', value: 50, discountAmount: 50 };

describe('DiscountCodeField', () => {
  it('checks the code against the subtotal and takes it off the total', async () => {
    const asked: string[] = [];
    returningCustomer();
    server.use(
      http.get(api('/orders/discount-codes/:code'), ({ request, params }) => {
        asked.push(`${params.code as string}${new URL(request.url).search}`);
        return json(oak50);
      }),
    );
    renderWithStore(<Summary />, { user });
    await screen.findAllByText('$320.00');

    await userEvent.type(screen.getByLabelText('Discount code'), 'oak50');
    await userEvent.click(screen.getByRole('button', { name: 'Apply' }));

    expect(await screen.findByText('Code OAK50')).toBeInTheDocument();
    expect(screen.getByText('$50.00 off')).toBeInTheDocument();
    expect(screen.getByText('$270.00')).toBeInTheDocument();
    expect(asked).toEqual(['OAK50?subtotal=320']);
  });

  it('shows why a code is refused next to the field, without a toast', async () => {
    returningCustomer();
    server.use(http.get(api('/orders/discount-codes/:code'), () => problemResponse(422, 'This code expired on Sep 1, 2026.')));
    renderWithStore(<Summary />, { user });
    await screen.findAllByText('$320.00');

    await userEvent.type(screen.getByLabelText('Discount code'), 'SUMMER25');
    await userEvent.click(screen.getByRole('button', { name: 'Apply' }));

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('This code expired on Sep 1, 2026.');
    expect(screen.getAllByText('This code expired on Sep 1, 2026.')).toHaveLength(1);
    expect(screen.getByLabelText('Discount code')).toHaveAttribute('aria-invalid', 'true');
    expect(screen.queryByText(/^Code /)).not.toBeInTheDocument();
  });

  it('keeps a smaller code unused on a first order and says so', async () => {
    server.use(http.get(api('/orders/discount-codes/:code'), () => json(oak50)));
    renderWithStore(<Summary />, { user });
    await screen.findAllByText('$320.00');

    await userEvent.type(screen.getByLabelText('Discount code'), 'OAK50');
    await userEvent.click(screen.getByRole('button', { name: 'Apply' }));

    // 20 % of 320 is 64, more than the code's 50
    expect(await screen.findByText('First order discount')).toBeInTheDocument();
    expect(await screen.findByText(/OAK50 is kept for another order/)).toBeInTheDocument();
  });

  it('removes an accepted code', async () => {
    returningCustomer();
    server.use(http.get(api('/orders/discount-codes/:code'), () => json(oak50)));
    const { store } = renderWithStore(<Summary />, { user });
    await screen.findAllByText('$320.00');
    await userEvent.type(screen.getByLabelText('Discount code'), 'OAK50');
    await userEvent.click(screen.getByRole('button', { name: 'Apply' }));

    await userEvent.click(await screen.findByRole('button', { name: 'Remove code OAK50' }));

    expect(screen.getByLabelText('Discount code')).toHaveValue('');
    expect(store.getState().discountCode.code).toBeNull();
  });
});

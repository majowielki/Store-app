import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { cartApi } from '@/api/cart';
import type { CreateOrderFromCartRequest } from '@/api/types';
import { order, user } from '@/test/fixtures';
import { api, json, problemResponse } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import CheckoutForm from './CheckoutForm';

const App = () => (
  <Routes>
    <Route path="/checkout" element={<CheckoutForm />} />
    <Route path="/orders" element={<h1>Your Orders</h1>} />
  </Routes>
);

describe('CheckoutForm', () => {
  it('places the order with an idempotency key, empties the cached cart and keeps the address on the profile', async () => {
    const requests: { key: string | null; body: CreateOrderFromCartRequest }[] = [];
    server.use(
      http.post(api('/orders/from-cart'), async ({ request }) => {
        requests.push({ key: request.headers.get('Idempotency-Key'), body: (await request.json()) as CreateOrderFromCartRequest });
        return json(order(), { status: 201 });
      }),
    );
    const { store } = renderWithStore(<App />, { user, route: '/checkout' });
    await store.dispatch(cartApi.endpoints.getCart.initiate());

    await userEvent.clear(screen.getByLabelText('address'));
    await userEvent.type(screen.getByLabelText('address'), 'New Street 5');
    await userEvent.click(screen.getByLabelText('save address to my profile'));
    await userEvent.click(screen.getByRole('button', { name: /place your order/i }));

    expect(await screen.findByRole('heading', { name: 'Your Orders' })).toBeInTheDocument();
    expect(requests).toHaveLength(1);
    expect(requests[0].key).toMatch(/^[0-9a-f-]{36}$/);
    expect(requests[0].body).toEqual({ customerName: user.userName, deliveryAddress: 'New Street 5', saveAddress: true });
    // The identity service stores the address later, through an event; the profile shown is updated now
    expect(store.getState().session.user?.simpleAddress).toBe('New Street 5');
    expect(cartApi.endpoints.getCart.select()(store.getState()).data?.items).toEqual([]);
  });

  it('shows the problem and stays on the page when the order is refused', async () => {
    server.use(http.post(api('/orders/from-cart'), () => problemResponse(409, 'Oak Table is no longer available')));
    const { store } = renderWithStore(<App />, { user, route: '/checkout' });

    await userEvent.click(screen.getByRole('button', { name: /place your order/i }));

    expect(await screen.findByText('Oak Table is no longer available')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /place your order/i })).toBeInTheDocument();
    expect(store.getState().session.user?.simpleAddress).toBe(user.simpleAddress);
  });

  it('does not offer to change the shared profile of a demo account', () => {
    renderWithStore(<App />, { user: { ...user, isDemo: true }, route: '/checkout' });

    expect(screen.getByLabelText('address')).toHaveAttribute('readonly');
    expect(screen.getByLabelText('user name')).toHaveAttribute('readonly');
    expect(screen.queryByLabelText('save address to my profile')).not.toBeInTheDocument();
  });
});

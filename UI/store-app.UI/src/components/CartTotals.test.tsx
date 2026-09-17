import { screen, waitFor } from '@testing-library/react';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import { user } from '@/test/fixtures';
import { api, json, problemResponse } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import CartTotals from './CartTotals';

const row = (label: string) => screen.getByText(label).nextElementSibling?.textContent;

describe('CartTotals', () => {
  it('shows the first-order discount only to a signed-in customer without orders', async () => {
    renderWithStore(<CartTotals />, { user });

    // The server cart (320) arrives, then has-orders says this is the first order
    expect(await screen.findByText('First order discount')).toBeInTheDocument();
    expect(row('Subtotal')).toBe('$320.00');
    expect(row('First order discount')).toBe('-$64.00');
    expect(row('Delivery')).toBe('$0.00');
    expect(row('Order Total')).toBe('$256.00');
  });

  it('shows no discount once the customer has ordered before', async () => {
    server.use(http.get(api('/orders/has-orders'), () => json({ hasOrders: true, ordersCount: 3 })));
    renderWithStore(<CartTotals />, { user });

    await waitFor(() => expect(row('Subtotal')).toBe('$320.00'));
    expect(screen.queryByText('First order discount')).not.toBeInTheDocument();
    expect(row('Order Total')).toBe('$320.00');
  });

  it('prices the guest cart from the browser with the published rules and charges delivery below the threshold', async () => {
    const lamp = { productId: 1, title: 'Lamp', image: '', company: 'x', color: 'red', unitPrice: 45.5, quantity: 2 };
    renderWithStore(<CartTotals />, { preloadedState: { guestCart: { items: [lamp] } } });

    await waitFor(() => expect(row('Delivery')).toBe('$10.00'));
    expect(row('Subtotal')).toBe('$91.00');
    expect(row('Order Total')).toBe('$101.00');
    expect(screen.queryByText('First order discount')).not.toBeInTheDocument();
  });
});

describe('CartTotals without the pricing rules', () => {
  it('shows only the subtotal until the store publishes its rules', async () => {
    const lamp = { productId: 1, title: 'Lamp', image: '', company: 'x', color: 'red', unitPrice: 45.5, quantity: 2 };
    server.use(http.get(api('/orders/pricing-rules'), () => problemResponse(503, 'Not now')));
    renderWithStore(<CartTotals />, { preloadedState: { guestCart: { items: [lamp] } } });

    expect(row('Subtotal')).toBe('$91.00');
    expect(await screen.findByText(/shown at checkout/i)).toBeInTheDocument();
    expect(screen.queryByText('Order Total')).not.toBeInTheDocument();
  });
});

import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import { user } from '@/test/fixtures';
import { api, problemResponse } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import Cart from './Cart';

const lamp = { productId: 1, title: 'Desk Lamp', image: '', company: 'x', color: 'red', unitPrice: 45.5, quantity: 2 };

describe('Cart page', () => {
  it('lists the guest cart from the browser and asks a visitor to sign in', () => {
    renderWithStore(<Cart />, { preloadedState: { guestCart: { items: [lamp] } } });

    expect(screen.getByRole('heading', { name: /shopping cart/i })).toBeInTheDocument();
    expect(screen.getByText('Desk Lamp')).toBeInTheDocument();
    expect(screen.getByText('Total price:').nextElementSibling).toHaveTextContent('$91.00');
    expect(screen.getByRole('link', { name: /please login/i })).toHaveAttribute('href', '/login');
  });

  it('removes a guest line without any request', async () => {
    const { store } = renderWithStore(<Cart />, { preloadedState: { guestCart: { items: [lamp] } } });

    await userEvent.click(screen.getByRole('button', { name: 'remove' }));

    expect(store.getState().guestCart.items).toEqual([]);
    expect(screen.getByRole('heading', { name: /empty cart/i })).toBeInTheDocument();
  });

  it('lists the server cart for a signed-in user and offers the checkout', async () => {
    renderWithStore(<Cart />, { user });

    expect(await screen.findByText('Oak Table')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /proceed to checkout/i })).toHaveAttribute('href', '/checkout');
  });

  it('removes a server line through the API and shows the cart the server answers with', async () => {
    renderWithStore(<Cart />, { user });
    await screen.findByText('Oak Table');

    await userEvent.click(screen.getByRole('button', { name: 'remove' }));

    expect(await screen.findByRole('heading', { name: /empty cart/i })).toBeInTheDocument();
    expect(await screen.findByText('Item removed from the cart')).toBeInTheDocument();
  });

  it('keeps the line and reports the problem when the server refuses', async () => {
    server.use(http.delete(api('/cart/items/:id'), () => problemResponse(409, 'The cart changed in the meantime')));
    renderWithStore(<Cart />, { user });
    await screen.findByText('Oak Table');

    await userEvent.click(screen.getByRole('button', { name: 'remove' }));

    expect(await screen.findByText('The cart changed in the meantime')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByText('Oak Table')).toBeInTheDocument());
  });
});

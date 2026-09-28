import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import { product, user as customer } from '@/test/fixtures';
import { api, problemResponse } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import ProductCard from './ProductCard';
import NotifyWhenBack from './NotifyWhenBack';

const soldOut = product({ availability: 'outOfStock', availableQuantity: 0 });

describe('stock on the product card', () => {
  it('says how many are left when few are, and when none are', () => {
    const { unmount } = renderWithStore(<ProductCard product={product({ availability: 'lowStock', availableQuantity: 2 })} />);
    expect(screen.getByText('Only 2 left')).toBeInTheDocument();
    unmount();

    renderWithStore(<ProductCard product={soldOut} />);
    expect(screen.getByText('Sold out')).toBeInTheDocument();
  });

  it('offers to write when a sold-out product is back instead of adding it to the bag', async () => {
    const user = userEvent.setup();
    renderWithStore(<ProductCard product={soldOut} />);

    await user.click(screen.getByRole('button', { name: 'Quick view: Oak Table' }));
    const window = await screen.findByRole('dialog', { name: 'Oak Table' });

    expect(window).not.toHaveTextContent('Add to bag');
    expect(screen.getByRole('form', { name: 'Notify me when it is back' })).toBeInTheDocument();
  });
});

describe('NotifyWhenBack', () => {
  it('fills in the address of the signed-in customer and confirms the request', async () => {
    const user = userEvent.setup();
    let sent: unknown;
    server.use(
      http.post(api('/products/7/notify'), async ({ request }) => {
        sent = await request.json();
        return new Response(null, { status: 204 });
      }),
    );
    renderWithStore(<NotifyWhenBack productId={7} />, { user: customer });

    expect(screen.getByRole('textbox', { name: 'E-mail address' })).toHaveValue('anna@example.com');
    await user.click(screen.getByRole('button', { name: 'Notify me' }));

    expect(await screen.findByRole('status')).toHaveTextContent('We will write to anna@example.com as soon as it is back.');
    expect(sent).toEqual({ email: 'anna@example.com' });
  });

  it('tells the visitor when the product came back in the meantime', async () => {
    const user = userEvent.setup();
    server.use(http.post(api('/products/7/notify'), () => problemResponse(409, '"Oak Table" is in stock')));
    renderWithStore(<NotifyWhenBack productId={7} />);

    await user.type(screen.getByRole('textbox', { name: 'E-mail address' }), 'visitor@example.com');
    await user.click(screen.getByRole('button', { name: 'Notify me' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('It is back in stock');
  });
});

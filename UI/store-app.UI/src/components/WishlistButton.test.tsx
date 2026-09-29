import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import { product, user } from '@/test/fixtures';
import { api, json } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import WishlistButton from './WishlistButton';
import WishlistLink from './WishlistLink';

const Heart = () => (
  <>
    <WishlistButton product={product()} />
    <WishlistLink />
  </>
);

describe('WishlistButton', () => {
  it('keeps a visitor\'s list in the browser and says whether the piece is on it', async () => {
    const { store } = renderWithStore(<Heart />);

    await userEvent.click(screen.getByRole('button', { name: 'Save Oak Table to your wishlist' }));

    expect(screen.getByRole('button', { name: 'Remove Oak Table from your wishlist' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('link', { name: 'Wishlist, 1 item' })).toBeInTheDocument();
    expect(store.getState().guestWishlist.productIds).toEqual([7]);

    await userEvent.click(screen.getByRole('button', { name: 'Remove Oak Table from your wishlist' }));
    expect(store.getState().guestWishlist.productIds).toEqual([]);
  });

  it('saves to the account of a signed-in customer', async () => {
    const posted: unknown[] = [];
    server.use(
      http.post(api('/wishlist/items'), async ({ request }) => {
        posted.push(await request.json());
        return json({ items: [{ productId: 7, addedAt: '2026-01-01T00:00:00Z' }] });
      }),
    );
    const { store } = renderWithStore(<Heart />, { user });

    await userEvent.click(await screen.findByRole('button', { name: 'Save Oak Table to your wishlist' }));

    expect(await screen.findByRole('button', { name: 'Remove Oak Table from your wishlist' })).toHaveAttribute('aria-pressed', 'true');
    expect(posted).toEqual([{ productId: 7 }]);
    expect(store.getState().guestWishlist.productIds).toEqual([]);
  });
});

import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import type { GuestCartItem } from '@/features/cart/guestCartSlice';
import { collection, product } from '@/test/fixtures';
import { api, json, page } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import CartDrawer from './CartDrawer';

const oakTable = product();
const pineChair = product({ id: 8, title: 'Pine Chair', slug: 'pine-chair', category: 'chairs', salePrice: null, effectivePrice: 90 });
const linenLamp = product({ id: 9, title: 'Linen Lamp', slug: 'linen-lamp', category: 'tableLamps', salePrice: null, effectivePrice: 60 });
const oakShelf = product({ id: 10, title: 'Oak Shelf', slug: 'oak-shelf', salePrice: null, effectivePrice: 150 });

const inBag: GuestCartItem = { productId: 7, title: 'Oak Table', image: oakTable.image, company: 'luxora', color: 'brown', unitPrice: 320, quantity: 1 };

const renderOpenDrawer = () =>
  renderWithStore(<CartDrawer />, {
    preloadedState: {
      guestCart: { items: [inBag] },
      cartDrawer: { open: true, lastAdded: { productId: 7, slug: 'oak-table', category: 'tables' } },
    },
  });

const suggestions = () => within(screen.getByRole('region', { name: 'Complete the look' })).getAllByRole('link').map((link) => link.textContent);

describe('CartDrawer', () => {
  it('shows the bag and completes the look from the collection the piece belongs to', async () => {
    server.use(
      http.get(api('/content/collections'), () => json([collection({ productSlugs: ['oak-table', 'pine-chair', 'linen-lamp'] })])),
      http.get(api('/products'), () => json(page([oakTable, pineChair, linenLamp]))),
    );
    renderOpenDrawer();

    const bag = screen.getByRole('dialog', { name: 'Added to your bag' });
    expect(within(bag).getByRole('list', { name: 'In your bag' })).toHaveTextContent('Oak Table');
    expect(within(bag).getByText('1 piece · $320.00')).toBeInTheDocument();
    expect(await within(bag).findByText('From the Warm Minimal collection')).toBeInTheDocument();
    // The table itself is in the bag already
    expect(suggestions()).toEqual([expect.stringContaining('Pine Chair'), expect.stringContaining('Linen Lamp')]);
  });

  it('falls back to more of the category when no collection holds the piece', async () => {
    const queries: string[] = [];
    server.use(
      http.get(api('/content/collections'), () => json([collection({ productSlugs: ['pine-chair'] })])),
      http.get(api('/products'), ({ request }) => {
        queries.push(new URL(request.url).search);
        return json(page([oakTable, oakShelf]));
      }),
    );
    renderOpenDrawer();

    expect(await screen.findByText('More like this')).toBeInTheDocument();
    expect(suggestions()).toEqual([expect.stringContaining('Oak Shelf')]);
    expect(queries).toContain('?category=tables&pageSize=12');
  });

  it('turns to the category when the rest of the collection is in the bag already', async () => {
    server.use(
      http.get(api('/content/collections'), () => json([collection({ productSlugs: ['oak-table'] })])),
      http.get(api('/products'), ({ request }) =>
        json(page(new URL(request.url).searchParams.has('slugs') ? [oakTable] : [oakTable, oakShelf])),
      ),
    );
    renderOpenDrawer();

    expect(await screen.findByText('More like this')).toBeInTheDocument();
    expect(suggestions()).toEqual([expect.stringContaining('Oak Shelf')]);
  });

  it('closes with Escape', async () => {
    server.use(
      http.get(api('/content/collections'), () => json([])),
      http.get(api('/products'), () => json(page([]))),
    );
    const { store } = renderOpenDrawer();

    await userEvent.keyboard('{Escape}');

    expect(store.getState().cartDrawer.open).toBe(false);
  });
});

import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { lookbook, product } from '@/test/fixtures';
import { renderWithStore } from '@/test/render';
import LookbookPicture, { AddLookButton } from './LookbookPicture';

const oakTable = product();
const pineChair = product({ id: 8, title: 'Pine Chair', slug: 'pine-chair', salePrice: null, effectivePrice: 90, colors: ['white', 'black'] });
const products = new Map([oakTable, pineChair].map((p) => [p.slug, p]));

const renderPicture = (available = products) =>
  renderWithStore(<LookbookPicture image="https://images.example.com/look.webp" alt="A dining room" hotspots={lookbook().hotspots} products={available} />);

describe('LookbookPicture', () => {
  it('draws a point for every product it knows, named with its price', () => {
    renderPicture(new Map([[oakTable.slug, oakTable]]));

    expect(screen.getByRole('button', { name: 'Oak Table, $320.00' })).toBeInTheDocument();
    // The chair is not in the catalogue any more: no point leads nowhere
    expect(screen.queryByRole('button', { name: /Pine Chair/ })).not.toBeInTheDocument();
  });

  it('opens a card from the keyboard and closes it with Escape', async () => {
    const user = userEvent.setup();
    renderPicture();

    await user.tab();
    expect(screen.getByRole('button', { name: 'Oak Table, $320.00' })).toHaveFocus();
    await user.keyboard('{Enter}');
    const card = screen.getByRole('dialog', { name: 'Oak Table' });
    expect(card).toHaveTextContent('$320.00');
    expect(screen.getByRole('link', { name: /Oak Table/ })).toHaveAttribute('href', '/products/7');
    expect(screen.getByRole('button', { name: 'Oak Table, $320.00' })).toHaveAttribute('aria-expanded', 'true');

    await user.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('shows one card at a time', async () => {
    const user = userEvent.setup();
    renderPicture();

    await user.click(screen.getByRole('button', { name: 'Oak Table, $320.00' }));
    await user.click(screen.getByRole('button', { name: 'Pine Chair, $90.00' }));

    expect(screen.getAllByRole('dialog')).toHaveLength(1);
    expect(screen.getByRole('dialog', { name: 'Pine Chair' })).toBeInTheDocument();
  });
});

describe('AddLookButton', () => {
  it('puts one of every piece in the bag, in its first colour', async () => {
    const user = userEvent.setup();
    const { store } = renderWithStore(<AddLookButton products={[oakTable, pineChair]} />);

    await user.click(screen.getByRole('button', { name: 'Add the whole look · $410.00' }));

    expect(await screen.findByText('2 pieces added to your bag.', { exact: true })).toBeInTheDocument();
    expect(store.getState().guestCart.items.map(({ productId, color, quantity, unitPrice }) => ({ productId, color, quantity, unitPrice }))).toEqual([
      { productId: 7, color: 'natural-oak', quantity: 1, unitPrice: 320 },
      { productId: 8, color: 'white', quantity: 1, unitPrice: 90 },
    ]);
  });
});

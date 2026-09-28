import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useLocation } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { product } from '@/test/fixtures';
import { renderWithStore } from '@/test/render';
import ProductCard from './ProductCard';

const Where = () => <p data-testid="where">{useLocation().pathname}</p>;

describe('QuickView', () => {
  it('opens from the keyboard and puts the product in the bag in the chosen colour', async () => {
    const user = userEvent.setup();
    const { store } = renderWithStore(
      <>
        <ProductCard product={product()} />
        <Where />
      </>,
      { route: '/products' },
    );

    const button = screen.getByRole('button', { name: 'Quick view: Oak Table' });
    button.focus();
    await user.keyboard('{Enter}');
    const window = await screen.findByRole('dialog', { name: 'Oak Table' });
    expect(window).toHaveTextContent('A sturdy oak table for six.');

    await user.click(await screen.findByRole('radio', { name: 'Black steel and oak' }));
    await user.click(screen.getByRole('button', { name: 'Add to bag' }));

    expect(store.getState().guestCart.items).toEqual([
      expect.objectContaining({ productId: 7, color: 'black-steel-oak', quantity: 1, unitPrice: 320 }),
    ]);
    expect(store.getState().cartDrawer).toEqual({ open: true, lastAdded: { productId: 7, slug: 'oak-table', category: 'tables' } });
    expect(screen.queryByRole('dialog', { name: 'Oak Table' })).not.toBeInTheDocument();
    // The visitor stayed on the listing
    expect(screen.getByTestId('where')).toHaveTextContent('/products');
  });

  it('sits next to the card link, not inside it', () => {
    renderWithStore(<ProductCard product={product()} />);

    const link = screen.getByRole('link', { name: /Oak Table/ });
    expect(link).not.toContainElement(screen.getByRole('button', { name: 'Quick view: Oak Table' }));
  });
});

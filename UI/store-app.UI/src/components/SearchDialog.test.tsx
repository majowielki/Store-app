import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import type { ProductSuggestions } from '@/api/types';
import { product } from '@/test/fixtures';
import { api, json } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import SearchDialog from './SearchDialog';

const sofa = product({ id: 3, title: 'Linen Slipcover Sofa', slug: 'linen-slipcover-sofa' });

describe('the search box', () => {
  it('suggests products while typing and says when a mistyped search was corrected', async () => {
    const user = userEvent.setup();
    const asked: string[] = [];
    server.use(
      http.get(api('/products/suggest'), ({ request }) => {
        const q = new URL(request.url).searchParams.get('q') ?? '';
        asked.push(q);
        return json<ProductSuggestions>({ query: q, correction: 'sofa', totalCount: 1, products: [sofa] });
      }),
    );
    renderWithStore(<SearchDialog open onOpenChange={() => {}} />);

    await user.type(screen.getByRole('searchbox', { name: 'Search products' }), 'sfoa');

    expect(await screen.findByRole('link', { name: /Linen Slipcover Sofa/ })).toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent('Showing results for “sofa” — nothing matched “sfoa”.');
    expect(screen.getByText('1 product')).toBeInTheDocument();
    expect(asked.at(-1)).toBe('sfoa');
  });

  it('says so when even the correction finds nothing', async () => {
    const user = userEvent.setup();
    server.use(http.get(api('/products/suggest'), () => json<ProductSuggestions>({ query: 'qzx', totalCount: 0, products: [] })));
    renderWithStore(<SearchDialog open onOpenChange={() => {}} />);

    await user.type(screen.getByRole('searchbox', { name: 'Search products' }), 'qzx');

    expect(await screen.findByText(/Nothing matches “qzx” yet/)).toBeInTheDocument();
  });
});

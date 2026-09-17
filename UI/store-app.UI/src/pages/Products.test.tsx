import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import { product } from '@/test/fixtures';
import { api, json, page } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import Products from './Products';

describe('Products page', () => {
  it('asks the catalogue for what the URL says and lists the answer', async () => {
    const queries: string[] = [];
    server.use(
      http.get(api('/products'), ({ request }) => {
        queries.push(new URL(request.url).search);
        return json(page([product({ id: 1, title: 'Oak Table' }), product({ id: 2, title: 'Pine Chair', salePrice: null, effectivePrice: 400 })]));
      }),
    );
    renderWithStore(<Products />, { route: '/products?search=oak&company=luxora&page=2&layout=list', path: '/products' });

    expect(await screen.findByText('Oak Table')).toBeInTheDocument();
    expect(screen.getByText('Pine Chair')).toBeInTheDocument();
    expect(screen.getByText('2 products')).toBeInTheDocument();
    // "layout" is the page's own; the rest goes to the API
    expect(queries).toEqual(['?search=oak&company=luxora&page=2']);
  });

  it('searches again from the filter form without leaving the page', async () => {
    const queries: string[] = [];
    server.use(
      http.get(api('/products'), ({ request }) => {
        queries.push(new URL(request.url).search);
        return json(page([product()]));
      }),
    );
    renderWithStore(<Products />, { route: '/products', path: '/products' });
    await screen.findByText('Oak Table');

    await userEvent.type(screen.getByLabelText('search product'), 'oak');
    await userEvent.click(screen.getByRole('checkbox', { name: 'sale only' }));
    await userEvent.click(screen.getByRole('button', { name: 'Search' }));

    await waitFor(() => expect(queries.at(-1)).toBe('?search=oak&price=0%2C2000&sale=on'));
  });

  it('says so when nothing matches', async () => {
    server.use(http.get(api('/products'), () => json(page([]))));
    renderWithStore(<Products />, { route: '/products?search=zzz', path: '/products' });

    expect(await screen.findByText(/no products matched/i)).toBeInTheDocument();
  });
});

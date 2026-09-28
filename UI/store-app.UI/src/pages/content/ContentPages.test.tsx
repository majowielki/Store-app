import { screen, within } from '@testing-library/react';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import { article, collection, maker, product } from '@/test/fixtures';
import { api, json, page, problemResponse } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import ArticlePage from './ArticlePage';
import CollectionPage from './CollectionPage';
import Journal from './Journal';
import MakerPage from './MakerPage';

const oakTable = product();
const pineChair = product({ id: 8, title: 'Pine Chair', slug: 'pine-chair', salePrice: null, effectivePrice: 90 });

describe('content pages', () => {
  it('shows a maker with the story and the products of its company', async () => {
    const queries: string[] = [];
    server.use(
      http.get(api('/content/makers/luxora'), () => json(maker())),
      http.get(api('/products'), ({ request }) => {
        queries.push(new URL(request.url).search);
        return json(page([oakTable]));
      }),
    );
    renderWithStore(<MakerPage />, { route: '/makers/luxora', path: '/makers/:slug' });

    expect(await screen.findByRole('heading', { level: 1, name: 'Luxora' })).toBeInTheDocument();
    expect(screen.getByText('Kraków · since 1998')).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 2, name: 'From the workshop' })).toBeInTheDocument();
    expect(await screen.findByText('Oak Table')).toBeInTheDocument();
    expect(queries).toEqual(['?company=luxora&pageSize=6']);
  });

  it('lists the products of a collection in the order the collection names them', async () => {
    const queries: string[] = [];
    server.use(
      http.get(api('/content/collections/warm-minimal'), () => json(collection())),
      http.get(api('/products'), ({ request }) => {
        queries.push(new URL(request.url).search);
        // The catalogue answers in its own order
        return json(page([oakTable, pineChair]));
      }),
    );
    renderWithStore(<CollectionPage />, { route: '/collections/warm-minimal', path: '/collections/:slug' });

    const products = await screen.findByRole('region', { name: 'Products in Warm Minimal' });
    const titles = within(products).getAllByRole('heading', { level: 3 }).map((heading) => heading.textContent);
    expect(titles).toEqual(['Pine Chair', 'Oak Table']);
    expect(queries).toEqual(['?slugs=pine-chair%2Coak-table&pageSize=100']);
  });

  it('leaves out a product the catalogue no longer lists', async () => {
    server.use(
      http.get(api('/content/collections/warm-minimal'), () => json(collection())),
      http.get(api('/products'), () => json(page([oakTable]))),
    );
    renderWithStore(<CollectionPage />, { route: '/collections/warm-minimal', path: '/collections/:slug' });

    const products = await screen.findByRole('region', { name: 'Products in Warm Minimal' });
    expect(within(products).getAllByRole('heading', { level: 3 }).map((heading) => heading.textContent)).toEqual(['Oak Table']);
  });

  it('answers an unknown address with a 404 page', async () => {
    server.use(http.get(api('/content/collections/nothing-here'), () => problemResponse(404, "Collection 'nothing-here' was not found")));
    renderWithStore(<CollectionPage />, { route: '/collections/nothing-here', path: '/collections/:slug' });

    expect(await screen.findByRole('heading', { name: 'Collection not found' })).toBeInTheDocument();
    expect(screen.getByText('404')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'All collections' })).toHaveAttribute('href', '/collections');
  });

  it('renders an article from Markdown and drops the raw HTML in it', async () => {
    server.use(
      http.get(api('/content/articles/caring-for-oak'), () =>
        json(article({ body: '## Oil, not polish\n\nA **thin** coat is enough.\n\n<script>alert(1)</script><b>raw</b>\n\n[See the tables](/products?category=tables)' })),
      ),
      http.get(api('/products'), () => json(page([oakTable]))),
    );
    const { container } = renderWithStore(<ArticlePage />, { route: '/journal/caring-for-oak', path: '/journal/:slug' });

    expect(await screen.findByRole('heading', { level: 1, name: 'Caring for oak' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 2, name: 'Oil, not polish' })).toBeInTheDocument();
    expect(screen.getByText('thin').tagName).toBe('STRONG');
    // The one script on the page is its structured data for search engines, never one from the text
    expect(container.querySelector('script:not([type="application/ld+json"])')).toBeNull();
    expect(container.querySelector('b')).toBeNull();
    const [data, trail] = JSON.parse(container.querySelector('script[type="application/ld+json"]')!.textContent!);
    expect(data).toMatchObject({ '@type': 'Article', headline: 'Caring for oak', author: { name: article().author } });
    expect(trail.itemListElement.map((step: { name: string }) => step.name)).toEqual(['Home', 'Journal', 'Caring for oak']);
    expect(document.title).toBe('Caring for oak — Store');
    // A link inside the shop stays in the app
    expect(screen.getByRole('link', { name: 'See the tables' })).toHaveAttribute('href', '/products?category=tables');
    expect(within(await screen.findByRole('region', { name: 'Products in this article' })).getByText('Oak Table')).toBeInTheDocument();
  });

  it('lists the journal with the date of every article', async () => {
    server.use(
      http.get(api('/content/articles'), () =>
        json([article(), article({ id: 2, slug: 'small-flat', title: 'Small flat, big ideas', publishedAt: '2026-08-27T12:00:00Z' })]),
      ),
    );
    renderWithStore(<Journal />, { route: '/journal', path: '/journal' });

    expect(await screen.findByRole('link', { name: /Caring for oak/ })).toHaveAttribute('href', '/journal/caring-for-oak');
    expect(screen.getByRole('link', { name: /Small flat, big ideas/ })).toHaveAttribute('href', '/journal/small-flat');
    expect(screen.getByText('Sep 1, 2026')).toBeInTheDocument();
  });
});

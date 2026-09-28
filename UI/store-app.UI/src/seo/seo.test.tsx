import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { productDetail } from '@/test/fixtures';
import JsonLd from './JsonLd';
import PageMeta from './PageMeta';
import { SITE_DESCRIPTION, SITE_TITLE } from './site';
import { productData } from './structuredData';
import type { PageMetaProps } from './usePageMeta';

const head = (selector: string) => document.head.querySelector(selector);
const meta = (key: string) => (head(`meta[name="${key}"]`) ?? head(`meta[property="${key}"]`))?.getAttribute('content') ?? null;

const renderPage = (props: PageMetaProps, route = '/products/7') =>
  render(
    <MemoryRouter initialEntries={[route]}>
      <PageMeta {...props} />
    </MemoryRouter>,
  );

describe('PageMeta', () => {
  it("writes the page's title, description, canonical address and social card", () => {
    renderPage({ title: 'Oak Table', description: 'A sturdy oak table for six.', image: '/pictures/oak.webp', kind: 'product' }, '/products/7?color=oak');

    expect(document.title).toBe('Oak Table — Store');
    expect(meta('description')).toBe('A sturdy oak table for six.');
    // The query is no part of the page's address for search engines
    expect(head('link[rel="canonical"]')).toHaveAttribute('href', `${window.location.origin}/products/7`);
    expect(meta('og:title')).toBe('Oak Table');
    expect(meta('og:type')).toBe('product');
    expect(meta('og:image')).toBe(`${window.location.origin}/pictures/oak.webp`);
    expect(meta('og:image:width')).toBeNull();
    expect(meta('robots')).toBeNull();
  });

  it('cuts a long description at a word, the length search results show', () => {
    renderPage({ title: 'Long', description: `${'word '.repeat(60)}end` });

    const description = meta('description')!;
    expect(description.length).toBeLessThanOrEqual(160);
    expect(description).toMatch(/word…$/);
  });

  it('keeps a private page out of search engines, with no canonical address', () => {
    renderPage({ title: 'Checkout', noindex: true }, '/checkout');

    expect(meta('robots')).toBe('noindex, follow');
    expect(head('link[rel="canonical"]')).toBeNull();
  });

  it("gives the shop's own values back when the page goes", () => {
    const { unmount } = renderPage({ title: 'Checkout', description: 'Pay for the bag.', noindex: true }, '/checkout');

    unmount();

    expect(document.title).toBe(SITE_TITLE);
    expect(meta('description')).toBe(SITE_DESCRIPTION);
    expect(meta('robots')).toBeNull();
    expect(meta('og:image')).toBe(`${window.location.origin}/og-image.jpg`);
    expect(meta('og:image:width')).toBe('1200');
  });
});

describe('structured data', () => {
  it('describes a product with the price it sells at, its availability and its rating', () => {
    const data = productData(productDetail({ effectivePrice: 339.5, availability: 'lowStock', ratingAverage: 4.5, ratingCount: 12 }), '/products/7');

    expect(data).toMatchObject({
      '@type': 'Product',
      name: 'Oak Table',
      brand: { name: 'Luxora' },
      offers: { price: '339.50', priceCurrency: 'USD', availability: 'https://schema.org/LimitedAvailability', url: `${window.location.origin}/products/7` },
      aggregateRating: { ratingValue: 4.5, reviewCount: 12 },
    });
    expect(data.image).toHaveLength(3);
  });

  it('leaves the rating out while a product has no reviews', () => {
    expect(productData(productDetail({ ratingCount: 0 }), '/products/7')).not.toHaveProperty('aggregateRating');
  });

  it('cannot be closed early by the text it carries', () => {
    const { container } = render(<JsonLd data={{ name: '</script><script>alert(1)</script>' }} />);

    const scripts = container.querySelectorAll('script');
    expect(scripts).toHaveLength(1);
    expect(JSON.parse(scripts[0].textContent!)).toEqual({ name: '</script><script>alert(1)</script>' });
  });
});

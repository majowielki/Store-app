import { expect, test } from '@playwright/test';
import { findProducts } from './helpers';

/** The addresses a sitemap lists (the e2e project has no DOM parser; the format is plain). */
const locations = (xml: string) => [...xml.matchAll(/<loc>([^<]+)<\/loc>/g)].map((match) => match[1]);

// Only the container (nginx) serves robots.txt and the sitemaps; the Vite dev server has the raw files
const servedByTheContainer = !!process.env.E2E_BASE_URL;

test('22. search engines find the shop: robots.txt, the sitemaps of every page and the data of a product', async ({ page, request }) => {
  test.skip(!servedByTheContainer, 'robots.txt and the sitemaps come from the UI container (E2E_BASE_URL)');

  const robots = await (await request.get('/robots.txt')).text();
  expect(robots).toMatch(/^Disallow: \/checkout$/m);
  const index = robots.match(/^Sitemap: (\S+)$/m)?.[1];
  expect(index).toMatch(/^https?:\/\/.+\/sitemap\.xml$/);

  // The index lists the UI's own pages, the catalogue and the editorial pages, all absolute
  const sitemaps = locations(await (await request.get(new URL(index!).pathname)).text());
  expect(sitemaps.map((address) => new URL(address).pathname)).toEqual(['/sitemap-pages.xml', '/sitemap-products.xml', '/sitemap-content.xml']);
  const pages = await Promise.all(sitemaps.map(async (address) => locations(await (await request.get(new URL(address).pathname)).text())));
  const [product] = await findProducts(request, { pageSize: '1' });
  expect(pages[0].map((address) => new URL(address).pathname)).toContain('/journal');
  expect(pages[1].map((address) => new URL(address).pathname)).toContain(`/products/${product.id}`);
  expect(pages[2].map((address) => new URL(address).pathname)).toContain('/journal/caring-for-oak');

  // A product's page names itself, points search engines at its address and describes its offer
  await page.goto(`/products/${product.id}?color=${encodeURIComponent(product.colors[0])}`);
  await expect(page.getByRole('heading', { level: 1, name: product.title })).toBeVisible();
  await expect(page).toHaveTitle(`${product.title} — Store`);
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', new RegExp(`/products/${product.id}$`));
  const data = JSON.parse((await page.locator('script[type="application/ld+json"]').textContent())!) as Record<string, unknown>[];
  expect(data.map((thing) => thing['@type'])).toEqual(['Product', 'BreadcrumbList']);
  expect(data[0]).toMatchObject({ name: product.title, offers: { priceCurrency: 'USD', price: product.effectivePrice.toFixed(2) } });

  // A private page asks not to be listed
  await page.goto('/cart');
  await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', 'noindex, follow');
  await expect(page.locator('link[rel="canonical"]')).toHaveCount(0);
});

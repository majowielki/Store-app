import { expect, test, type Page } from '@playwright/test';
import type { Funnel, OrderStatsResponse } from '../src/api/types';
import { addToCart, asTrueAdmin, findProducts, login, placeOrder, register, trueAdmin, uniqueEmail } from './helpers';

const dashboard = async (page: Page) => {
  const headers = await asTrueAdmin(page.request);
  await page.goto('/admin');
  // SignalR can replace a browser fetch during navigation. Read stable API
  // responses here; below we independently verify the values rendered by the UI.
  const [statsResponse, funnelResponse] = await Promise.all([
    page.request.get('/api/v1/admin/orders/stats?days=30', { headers }),
    page.request.get('/api/v1/auditlog/funnel?days=30', { headers }),
  ]);
  expect(statsResponse.ok()).toBeTruthy();
  expect(funnelResponse.ok()).toBeTruthy();
  const stats = await statsResponse.json() as OrderStatsResponse;
  const funnel = await funnelResponse.json() as Funnel;
  expect(funnel.days).toBe(30);
  return { stats, funnel };
};

test('26. the purchase funnel and dashboard count the same orders before and after checkout', async ({ page, browser, request }) => {
  const [product] = await findProducts(request, { pageSize: '1' });
  const adminContext = await browser.newContext();
  try {
    const admin = await adminContext.newPage();
    await login(admin, trueAdmin.email, trueAdmin.password);

    // The audit projection is asynchronous. Re-read until it has caught up, rather than
    // assuming that a SignalR notification means the independent audit consumer committed.
    let baseline = await dashboard(admin);
    await expect(async () => {
      baseline = await dashboard(admin);
      expect(baseline.funnel.stages.find((stage) => stage.stage === 'orderPlaced')?.count).toBe(baseline.stats.totalOrders);
    }).toPass({ timeout: 20_000 });

    await register(page, uniqueEmail('funnel'));
    await addToCart(page, product.id);
    await placeOrder(page);

    await expect(async () => {
      const { stats, funnel } = await dashboard(admin);
      const count = (stage: string) => funnel.stages.find((entry) => entry.stage === stage)!.count;
      const before = (stage: string) => baseline.funnel.stages.find((entry) => entry.stage === stage)!.count;
      expect(stats.totalOrders).toBe(baseline.stats.totalOrders + 1);
      expect(count('orderPlaced')).toBe(stats.totalOrders);
      expect(count('productViewed')).toBeGreaterThan(before('productViewed'));
      expect(count('addedToBag')).toBeGreaterThan(before('addedToBag'));

      // Check rendered values too: an API-only comparison would miss a chart wired to
      // the wrong field. The order is deliberately unpaid; both metrics count placement.
      const ordersCard = admin.locator('.border').filter({ has: admin.getByText('Orders', { exact: true }) });
      await expect(ordersCard.locator('p').first()).toHaveText(String(stats.totalOrders));
      const orderStage = admin.getByRole('list', { name: 'Purchase funnel' }).getByRole('listitem').filter({ hasText: 'Orders placed' });
      await expect(orderStage.locator('span').last()).toHaveText(stats.totalOrders.toLocaleString('en-US'));
    }).toPass({ timeout: 20_000 });
  } finally {
    await adminContext.close();
  }
});

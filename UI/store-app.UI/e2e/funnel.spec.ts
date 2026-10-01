import { expect, test, type Page } from '@playwright/test';
import type { Funnel, OrderStatsResponse } from '../src/api/types';
import { addToCart, findProducts, login, placeOrder, register, trueAdmin, uniqueEmail } from './helpers';

const dashboard = async (page: Page) => {
  const statsResponse = page.waitForResponse((response) => response.url().includes('/admin/orders/stats?days=30') && response.ok());
  const funnelResponse = page.waitForResponse((response) => response.url().includes('/auditlog/funnel?days=30') && response.ok());
  await page.goto('/admin');
  const stats = (await (await statsResponse).json()) as OrderStatsResponse;
  const funnel = (await (await funnelResponse).json()) as Funnel;
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

import { expect, test } from '@playwright/test';
import { addToCart, asTrueAdmin, checkout, expectToast, findProducts, login, pay, placeOrder, register, trueAdmin, uniqueEmail } from './helpers';

test('11. the true administrator ships a paid order and the customer follows every step on the order page', async ({ page, browser, request }) => {
  const [product] = await findProducts(request, { pageSize: '1' });
  await register(page, uniqueEmail('status'));
  await addToCart(page, product.id);
  await expect(page.getByText(/^Arrives /)).toBeVisible();
  const orderId = await checkout(page);

  await page.goto(`/orders/${orderId}`);
  const progress = page.getByRole('list', { name: 'Order progress' });
  await expect(progress.getByRole('listitem')).toHaveCount(4);
  await expect(progress.getByRole('listitem').nth(2)).toContainText('Visa •••• 4242');
  await expect(progress.getByRole('listitem').nth(3)).toContainText('Not yet');
  await expect(page.getByText(/^Estimated delivery/)).toBeVisible();

  const adminContext = await browser.newContext();
  const admin = await adminContext.newPage();
  await login(admin, trueAdmin.email, trueAdmin.password);
  await admin.goto(`/admin/orders/${orderId}`);
  // Paying is the payment's business: the administrator only ships or cancels
  await expect(admin.getByRole('button', { name: 'Mark as paid' })).toHaveCount(0);
  await admin.getByRole('button', { name: 'Mark as shipped' }).click();
  await expectToast(admin, `Order #${orderId} is now shipped.`);
  await expect(admin.getByRole('button', { name: /Mark as|Cancel order/ })).toHaveCount(0);
  await adminContext.close();

  const refreshed = page.waitForResponse((response) => response.url().endsWith(`/api/v1/orders/${orderId}`) && response.ok());
  await page.reload();
  const placed = (await (await refreshed).json()) as { statusHistory: { changedAt: string }[] };
  const labels = ['Order placed', 'Awaiting payment', 'Payment received', 'Shipped'];
  await expect(progress.getByRole('listitem')).toHaveText(labels.map((label, i) => new RegExp(`${label}.*${new Date(placed.statusHistory[i].changedAt).getFullYear()}`)));
  await expect(page.getByText(/^On its way, arriving/)).toBeVisible();
  await page.goto('/orders');
  await expect(page.getByRole('row').nth(1)).toContainText('Shipped');
});

test('12. a returning customer applies a discount code in the cart and the order keeps it', async ({ page, request }) => {
  const [first] = await findProducts(request, { pageSize: '1' });
  const [big] = await findProducts(request, { price: '450,5000', pageSize: '1' });
  expect(big.effectivePrice).toBeGreaterThanOrEqual(400);

  const headers = await asTrueAdmin(request);
  const suffix = Date.now().toString(36).toUpperCase();
  const expiredCode = `EXPIRED-${suffix}`;
  const activeCode = `OAK-${suffix}`;
  const codeIds: number[] = [];
  try {
    for (const [code, expiresAt] of [[expiredCode, new Date(Date.now() - 86_400_000).toISOString()], [activeCode, null]] as const) {
      const response = await request.post('/api/v1/admin/discount-codes', { headers, data: { code, kind: 'Amount', value: 50, minimumSubtotal: 400, expiresAt, isActive: true } });
      expect(response.ok()).toBeTruthy();
      codeIds.push(((await response.json()) as { id: number }).id);
    }
    // The first order takes the first-order discount; the code is for the next one
    await register(page, uniqueEmail('code'));
    await addToCart(page, first.id);
    await checkout(page);

    await addToCart(page, big.id);
    await page.goto('/cart');
    const field = page.getByLabel('Discount code');
    await field.fill(expiredCode);
    await page.getByRole('button', { name: 'Apply' }).click();
    await expect(page.getByRole('alert')).toHaveText(/^This code expired on /);

    await field.fill(activeCode);
    await page.getByRole('button', { name: 'Apply' }).click();
    await expect(page.getByText(`Code ${activeCode}`)).toBeVisible();
    await expect(page.getByText('$50.00 off')).toBeVisible();

    await page.getByRole('link', { name: /Proceed to checkout/ }).click();
    await expect(page.getByText(`Code ${activeCode}`)).toBeVisible();
    // The typed code survives a reload of the tab
    await page.reload();
    await expect(page.getByText(`Code ${activeCode}`)).toBeVisible();
    const orderId = await placeOrder(page, 'E2E Street 2');
    await pay(page);

    await page.goto(`/orders/${orderId}`);
    await expect(page.getByText(`Code ${activeCode}:`)).toBeVisible();
    await expect(page.getByText('-$50.00')).toBeVisible();
  } finally {
    for (const [index, id] of codeIds.entries()) {
      const deleted = await request.delete(`/api/v1/admin/discount-codes/${id}`, { headers });
      if (deleted.status() === 409) {
        const disabled = await request.put(`/api/v1/admin/discount-codes/${id}`, { headers, data: { code: index === 0 ? expiredCode : activeCode, kind: 'Amount', value: 50, minimumSubtotal: 400, isActive: false } });
        expect(disabled.ok()).toBeTruthy();
      } else expect(deleted.ok()).toBeTruthy();
    }
  }
});

test('25. the administrator sees a new order and its payment on the list without reloading it', async ({ page, browser, request }) => {
  const [product] = await findProducts(request, { pageSize: '1' });
  const adminContext = await browser.newContext();
  const admin = await adminContext.newPage();
  await login(admin, trueAdmin.email, trueAdmin.password);
  await admin.goto('/admin/orders');
  await expect(admin.getByRole('status').filter({ hasText: 'Live' })).toBeVisible();

  await register(page, uniqueEmail('live'));
  await addToCart(page, product.id);
  const orderId = await placeOrder(page);

  // The list follows by itself, within two seconds of the order
  const row = admin.getByRole('row').filter({ has: admin.getByRole('cell', { name: String(orderId), exact: true }) });
  await expect(row).toBeVisible({ timeout: 2_000 });
  await expectToast(admin, `New order #${orderId}.`);

  await pay(page);
  await expect(row).toContainText('Paid', { timeout: 2_000 });
  await expectToast(admin, `Order #${orderId} is paid.`);
  await adminContext.close();
});

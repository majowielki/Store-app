import { expect, test } from '@playwright/test';
import { addToCart, checkout, expectToast, findProducts, login, register, trueAdmin, uniqueEmail } from './helpers';

test('11. the true administrator ships an order and the customer follows every step on the order page', async ({ page, browser, request }) => {
  const [product] = await findProducts(request, { pageSize: '1' });
  await register(page, uniqueEmail('status'));
  await addToCart(page, product.id);
  await expect(page.getByText(/^Arrives /)).toBeVisible();
  await checkout(page);

  await page.getByRole('row').nth(1).click();
  await expect(page).toHaveURL(/\/orders\/\d+$/);
  const orderId = page.url().split('/').at(-1)!;
  const progress = page.getByRole('list', { name: 'Order progress' });
  await expect(progress.getByRole('listitem')).toHaveCount(3);
  await expect(progress.getByRole('listitem').nth(1)).toContainText('Not yet');
  await expect(page.getByText(/^Estimated delivery/)).toBeVisible();

  const adminContext = await browser.newContext();
  const admin = await adminContext.newPage();
  await login(admin, trueAdmin.email, trueAdmin.password);
  await admin.goto(`/admin/orders/${orderId}`);
  await admin.getByRole('button', { name: 'Mark as paid' }).click();
  await expectToast(admin, `Order #${orderId} is now paid.`);
  await admin.getByRole('button', { name: 'Mark as shipped' }).click();
  await expectToast(admin, `Order #${orderId} is now shipped.`);
  await expect(admin.getByRole('button', { name: /Mark as|Cancel order/ })).toHaveCount(0);
  await adminContext.close();

  await page.reload();
  await expect(progress.getByRole('listitem')).toHaveText([/Order placed.*2026/, /Payment received.*2026/, /Shipped.*2026/]);
  await expect(page.getByText(/^On its way, arriving/)).toBeVisible();
  await page.goto('/orders');
  await expect(page.getByRole('row').nth(1)).toContainText('Shipped');
});

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

test('12. a returning customer applies a discount code in the cart and the order keeps it', async ({ page, request }) => {
  const [first] = await findProducts(request, { pageSize: '1' });
  const [big] = await findProducts(request, { price: '450,5000', pageSize: '1' });
  expect(big.effectivePrice).toBeGreaterThanOrEqual(400);

  // The first order takes the first-order discount; the code is for the next one
  await register(page, uniqueEmail('code'));
  await addToCart(page, first.id);
  await checkout(page);

  await addToCart(page, big.id);
  await page.goto('/cart');
  const field = page.getByLabel('Discount code');
  await field.fill('summer25');
  await page.getByRole('button', { name: 'Apply' }).click();
  await expect(page.getByRole('alert')).toHaveText('This code expired on Sep 1, 2026.');

  await field.fill('OAK50');
  await page.getByRole('button', { name: 'Apply' }).click();
  await expect(page.getByText('Code OAK50')).toBeVisible();
  await expect(page.getByText('$50.00 off')).toBeVisible();

  await page.getByRole('link', { name: /Proceed to checkout/ }).click();
  await expect(page.getByText('Code OAK50')).toBeVisible();
  // The typed code survives a reload of the tab
  await page.reload();
  await expect(page.getByText('Code OAK50')).toBeVisible();
  await page.getByLabel('address', { exact: true }).fill('E2E Street 2');
  await page.getByRole('button', { name: 'Place Your Order' }).click();
  await expectToast(page, 'Order placed');

  await page.getByRole('row').nth(1).click();
  await expect(page.getByText('Code OAK50:')).toBeVisible();
  await expect(page.getByText('-$50.00')).toBeVisible();
});

import { expect, test } from '@playwright/test';
import { addToCart, checkout, expectToast, findProducts, login, money, register, uniqueEmail } from './helpers';

// The six scenarios of the test plan, against a running stack. Each one registers its own
// customer where it needs one, so they can run in any order and more than once.

test('1. a visitor browses, filters by colour, opens a product and adds it to the cart', async ({ page, request }) => {
  const [gray] = await findProducts(request, { color: 'gray', pageSize: '1' });

  await page.goto('/products');
  await page.getByRole('combobox', { name: 'select color' }).click();
  await page.getByRole('option', { name: 'Gray' }).click();
  await page.getByRole('form', { name: 'Product filters' }).getByRole('button', { name: 'Search' }).click();

  await expect(page).toHaveURL(/color=gray/);
  await expect(page.getByRole('link', { name: gray.title })).toBeVisible();

  await page.getByRole('link', { name: gray.title }).click();
  await expect(page.getByRole('heading', { name: gray.title })).toBeVisible();
  await page.getByRole('button', { name: 'Add to bag' }).click();

  await expectToast(page, 'Item added to cart');
  await expect(page.getByRole('link', { name: 'Cart, 1 items' })).toBeVisible();
  await page.goto('/cart');
  await expect(page.getByRole('heading', { name: gray.title })).toBeVisible();
});

test('2. registering merges the guest cart, the checkout places the order and it shows under Orders', async ({ page, request }) => {
  const [product] = await findProducts(request, { pageSize: '1' });
  await addToCart(page, product.id, 2);

  await register(page, uniqueEmail('merge'));

  // The line added as a visitor is in the server cart now - once: two pieces, not four
  await page.goto('/cart');
  await expect(page.getByRole('heading', { name: product.title })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Cart, 2 items' })).toBeVisible();

  await checkout(page);

  await expect(page.getByRole('heading', { name: 'Your Orders' })).toBeVisible();
  await expect(page.getByText('total orders : 1')).toBeVisible();
  await expect(page.getByRole('link', { name: 'Cart, 0 items' })).toBeVisible();
});

test('3. a line removed from the cart is not in the order', async ({ page, request }) => {
  const [first, second] = await findProducts(request, { pageSize: '2' });
  await register(page, uniqueEmail('remove'));
  await addToCart(page, first.id);
  await addToCart(page, second.id);

  await page.goto('/cart');
  const secondLine = page.getByTestId('cart-line').filter({ hasText: second.title });
  await secondLine.getByRole('button', { name: 'remove' }).click();
  await expectToast(page, 'Item removed from the cart');
  await expect(page.getByRole('heading', { name: second.title })).toHaveCount(0);

  await checkout(page);
  await page.getByRole('button', { name: 'Open menu' }).first().click();
  await page.getByRole('menuitem', { name: 'Order details' }).click();

  await expect(page.getByRole('cell', { name: first.title })).toBeVisible();
  await expect(page.getByRole('cell', { name: second.title })).toHaveCount(0);
  await expect(page.getByText('Total Items: 1')).toBeVisible();
});

test('4. a product on sale costs the same on the list, in the cart and on the order', async ({ page, request }) => {
  const [sale] = await findProducts(request, { sale: 'true', pageSize: '1' });
  expect(sale.effectivePrice).toBeLessThan(sale.price);
  const price = money(sale.effectivePrice);

  await page.goto('/products?sale=true');
  const card = page.getByRole('link', { name: sale.title });
  await expect(card).toContainText(price);

  await register(page, uniqueEmail('sale'));
  await addToCart(page, sale.id);
  await page.goto('/cart');
  await expect(page.getByText(`Price: ${price}`)).toBeVisible();

  await checkout(page);
  await page.getByRole('button', { name: 'Open menu' }).first().click();
  await page.getByRole('menuitem', { name: 'Order details' }).click();
  await expect(page.getByRole('row', { name: new RegExp(sale.title) })).toContainText(price);
  await expect(page.getByText(`Subtotal: ${price}`)).toBeVisible();
});

test.describe('signing in again', () => {
  test('a registered customer can sign in with the password', async ({ page }) => {
    const email = uniqueEmail('login');
    await register(page, email);
    await page.goto('/');
    await page.getByRole('button', { name: 'My Account' }).click();
    await page.getByRole('menuitem', { name: 'Log out' }).click();
    await expectToast(page, 'Logged out');

    await login(page, email, 'E2e-Password-1!');
    await expect(page).toHaveURL(/\/$/);
  });
});

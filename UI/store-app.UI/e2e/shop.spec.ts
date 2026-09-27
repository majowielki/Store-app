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

  // The bag slides in with the line; Escape closes it and the focus is back on the button
  const bag = page.getByRole('dialog', { name: 'Added to your bag' });
  await expect(bag.getByRole('list', { name: 'In your bag' })).toContainText(gray.title);
  await page.keyboard.press('Escape');
  await expect(bag).toBeHidden();
  await expect(page.getByRole('button', { name: 'Add to bag' })).toBeFocused();
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

test('13. a quick view puts a product in the bag without leaving the catalogue', async ({ page }) => {
  await page.goto('/products');
  const quickView = page.getByRole('button', { name: /^Quick view: / }).first();
  const title = (await quickView.getAttribute('aria-label'))!.replace('Quick view: ', '');
  await quickView.focus();
  await page.keyboard.press('Enter');

  const window = page.getByRole('dialog', { name: title });
  await expect(window).toBeVisible();
  await window.getByRole('button', { name: 'Add to bag' }).click();

  const bag = page.getByRole('dialog', { name: 'Added to your bag' });
  await expect(bag.getByRole('list', { name: 'In your bag' })).toContainText(title);
  await expect(bag.getByRole('heading', { name: 'Complete the look' })).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(bag).toBeHidden();
  await expect(quickView).toBeFocused();
  await expect(page).toHaveURL(/\/products$/);
});

test('14. a visitor keeps pieces on the wishlist, signs up and finds them on the account', async ({ page }) => {
  await page.goto('/products');
  const heart = page.getByRole('button', { name: /^Save .* to your wishlist$/ }).first();
  const title = (await heart.getAttribute('aria-label'))!.replace(/^Save | to your wishlist$/g, '');
  await heart.click();
  await expect(page.getByRole('button', { name: `Remove ${title} from your wishlist` }).first()).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByRole('link', { name: 'Wishlist, 1 items' })).toBeVisible();

  // The list waits in the browser and joins the new account at sign-up
  await register(page, uniqueEmail('wishlist'));
  await page.goto('/wishlist');
  const list = page.getByRole('list', { name: 'Wishlist' });
  await expect(list).toContainText(title);
  await page.reload();
  await expect(list).toContainText(title);

  await list.getByRole('button', { name: 'Move to bag' }).click();
  const bag = page.getByRole('dialog', { name: 'Added to your bag' });
  await expect(bag.getByRole('list', { name: 'In your bag' })).toContainText(title);
  await page.keyboard.press('Escape');
  await expect(page.getByRole('heading', { name: 'Your wishlist is empty' })).toBeVisible();
});

test('15. a visitor compares pieces side by side and sees what they viewed before', async ({ page, request }) => {
  const [first, second] = await findProducts(request, { pageSize: '2' });
  await page.goto(`/products/${first.id}`);
  await expect(page.getByRole('heading', { level: 1, name: first.title })).toBeVisible();
  await page.goto(`/products/${second.id}`);
  const recent = page.getByRole('region', { name: 'Recently viewed' });
  await expect(recent).toContainText(first.title);
  await expect(recent).not.toContainText(second.title);

  await page.goto('/products');
  const toggles = page.getByRole('button', { name: /^Compare / });
  for (let i = 0; i < 4; i++) await toggles.nth(0).click(); // each press turns the next card's button on
  await expect(page.getByRole('button', { name: /^Remove .* from the comparison$/ })).toHaveCount(4 * 2); // card + bar
  await toggles.nth(0).click();
  await expectToast(page, `You can compare up to 4 pieces. Remove one to add ${(await toggles.nth(0).getAttribute('aria-label'))!.replace('Compare ', '')}.`);

  await page.getByRole('complementary', { name: 'Comparison' }).getByRole('link', { name: 'Compare 4' }).click();
  await expect(page).toHaveURL(/\/compare$/);
  await expect(page.getByRole('columnheader')).toHaveCount(4);
  await expect(page.getByRole('row', { name: /^Price/ })).toBeVisible();
});

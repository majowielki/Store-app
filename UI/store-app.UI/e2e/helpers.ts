import { expect, type APIRequestContext, type Page } from '@playwright/test';
import type { Product, ProductsResponse } from '../src/api/types';

export const API = '/api/v1';

/** The true administrator the stack was started with (docker-compose.yml defaults). */
export const trueAdmin = {
  email: process.env.E2E_ADMIN_EMAIL ?? 'trueadmin@store.com',
  password: process.env.E2E_ADMIN_PASSWORD ?? 'SecurePassword123!',
};

export const password = 'E2e-Password-1!';

export const uniqueEmail = (prefix: string) => `${prefix}-${Date.now()}-${Math.floor(Math.random() * 1e6)}@e2e.local`;

/** The first products the catalogue lists for a query, straight from the API, with stock enough to buy them. */
export const findProducts = async (request: APIRequestContext, query: Record<string, string>): Promise<Product[]> => {
  const response = await request.get(`${API}/products`, { params: query });
  expect(response.ok()).toBeTruthy();
  const products = ((await response.json()) as ProductsResponse).items;
  await restock(request, products);
  return products;
};

let adminToken: string | undefined;

/** The true administrator's authorization, for the setup a scenario makes through the API. */
const asTrueAdmin = async (request: APIRequestContext) => {
  if (!adminToken) {
    const response = await request.post(`${API}/auth/login`, { data: { email: trueAdmin.email, password: trueAdmin.password } });
    expect(response.ok()).toBeTruthy();
    adminToken = ((await response.json()) as { accessToken: string }).accessToken;
  }
  return { Authorization: `Bearer ${adminToken}` };
};

/**
 * Every run buys pieces that stay held - paid orders are never shipped - so the products the
 * scenarios pick run out after a few runs. The true administrator counts them back in first.
 */
export const restock = async (request: APIRequestContext, products: Pick<Product, 'id' | 'availableQuantity'>[]) => {
  const low = products.filter((product) => product.availableQuantity < 20);
  if (low.length === 0) return;
  const headers = await asTrueAdmin(request);
  for (const product of low) {
    const current = await request.get(`${API}/products/admin/${product.id}`, { headers });
    expect(current.ok()).toBeTruthy();
    const { reservedQuantity } = (await current.json()) as { reservedQuantity: number };
    const updated = await request.put(`${API}/products/${product.id}/stock`, { headers, data: { stockQuantity: (reservedQuantity ?? 0) + 40 } });
    expect(updated.ok()).toBeTruthy();
  }
};

export const register = async (page: Page, email: string) => {
  await page.goto('/register');
  await page.getByLabel('first name', { exact: true }).fill('E2e');
  await page.getByLabel('last name', { exact: true }).fill('Tester');
  await page.getByLabel('email', { exact: true }).fill(email);
  await page.getByLabel('password', { exact: true }).fill(password);
  await page.getByLabel('confirm password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Register' }).click();
  await expectToast(page, 'Successfully registered!');
};

export const login = async (page: Page, email: string, pass: string) => {
  await page.goto('/login');
  await page.getByLabel('email', { exact: true }).fill(email);
  await page.getByLabel('password', { exact: true }).fill(pass);
  await page.getByRole('button', { name: 'Login' }).click();
  await expectToast(page, 'Successfully logged in!');
};

export const loginAsDemoAdmin = async (page: Page) => {
  await page.goto('/login');
  await page.getByRole('button', { name: 'Demo Admin' }).click();
  await expectToast(page, 'Demo admin logged in!');
};

/** Opens the product page and adds it to the cart in its first colour. */
export const addToCart = async (page: Page, productId: number, quantity = 1) => {
  await page.goto(`/products/${productId}`);
  await expect(page.getByRole('button', { name: 'Add to bag' })).toBeVisible();
  if (quantity !== 1) {
    await page.getByRole('combobox').first().click();
    await page.getByRole('option', { name: String(quantity), exact: true }).click();
  }
  await page.getByRole('button', { name: 'Add to bag' }).click();
  await closeBag(page);
};

/** The bag slides in after "Add to bag"; Escape closes it and the focus goes back to the button. */
export const closeBag = async (page: Page) => {
  const bag = page.getByRole('dialog', { name: 'Added to your bag' });
  await expect(bag).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(bag).toBeHidden();
};

/** Places the order from the checkout page and lands on its payment page; returns the order id. */
export const placeOrder = async (page: Page, address = 'E2E Street 1'): Promise<number> => {
  await page.goto('/checkout');
  await page.getByLabel('address', { exact: true }).fill(address);
  await page.getByRole('button', { name: 'Continue to payment' }).click();
  await expectToast(page, 'Order placed');
  await expect(page).toHaveURL(/\/orders\/\d+\/pay$/);
  return Number(page.url().split('/').at(-2));
};

/** On the payment page: fills the form with a test card (its "Use" button) and presses Pay. */
export const payWith = async (page: Page, card: string) => {
  // The form appears once the pieces are reserved, a second or two after the order
  await page.getByRole('button', { name: `Use test card ${card}` }).click();
  await page.getByRole('button', { name: /^Pay \$/ }).click();
};

/** Pays with a card that goes through and waits for the shop to hear of it (a webhook away). */
export const pay = async (page: Page, card = '4242 4242 4242 4242') => {
  await payWith(page, card);
  await expect(page.getByText('Payment received', { exact: true })).toBeVisible({ timeout: 20_000 });
};

/** The whole purchase: places the order, pays it with 4242 and lands on the orders list; returns the order id. */
export const checkout = async (page: Page, address = 'E2E Street 1'): Promise<number> => {
  const orderId = await placeOrder(page, address);
  await pay(page);
  await page.goto('/orders');
  return orderId;
};

/** The toast text as shown (the aria-live announcement repeats it with a prefix). */
export const expectToast = (page: Page, text: string) => expect(page.getByText(text, { exact: true })).toBeVisible();

export const money = (amount: number) => `$${amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

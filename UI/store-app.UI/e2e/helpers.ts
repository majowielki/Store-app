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

/** The first products the catalogue lists for a query, straight from the API. */
export const findProducts = async (request: APIRequestContext, query: Record<string, string>): Promise<Product[]> => {
  const response = await request.get(`${API}/products`, { params: query });
  expect(response.ok()).toBeTruthy();
  return ((await response.json()) as ProductsResponse).items;
};

export const register = async (page: Page, email: string) => {
  await page.goto('/register');
  await page.getByLabel('firstName', { exact: true }).fill('E2e');
  await page.getByLabel('lastName', { exact: true }).fill('Tester');
  await page.getByLabel('email', { exact: true }).fill(email);
  await page.getByLabel('password', { exact: true }).fill(password);
  await page.getByLabel('confirmPassword', { exact: true }).fill(password);
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
  await expectToast(page, 'Item added to cart');
};

/** Places the order from the checkout page and lands on the orders list. */
export const checkout = async (page: Page, address = 'E2E Street 1') => {
  await page.goto('/checkout');
  await page.getByLabel('address', { exact: true }).fill(address);
  await page.getByRole('button', { name: 'Place Your Order' }).click();
  await expectToast(page, 'Order placed');
  await expect(page).toHaveURL(/\/orders$/);
};

/** The toast text as shown (the aria-live announcement repeats it with a prefix). */
export const expectToast = (page: Page, text: string) => expect(page.getByText(text, { exact: true })).toBeVisible();

export const money = (amount: number) => `$${amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

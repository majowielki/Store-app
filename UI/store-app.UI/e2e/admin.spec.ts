import { expect, test, type Page } from '@playwright/test';
import { expectToast, login, loginAsDemoAdmin, trueAdmin } from './helpers';

const fillProductForm = async (page: Page, title: string, price: string) => {
  await page.getByLabel('title', { exact: true }).fill(title);
  await page.getByLabel('company', { exact: true }).fill('luxora');
  await page.getByLabel('category', { exact: true }).fill('tables');
  await page.getByLabel('price', { exact: true }).fill(price);
  await page.getByLabel('image url', { exact: true }).fill('https://images.example.com/e2e-table.jpg');
  // A finish typed by its key, another picked from the swatches
  await page.getByLabel('colors (comma separated)', { exact: true }).fill('natural-oak');
  await page.getByRole('group', { name: 'finishes' }).getByRole('button', { name: 'Walnut', exact: true }).click();
  await expect(page.getByLabel('colors (comma separated)', { exact: true })).toHaveValue('natural-oak, walnut');
  await page.getByLabel('groups (comma separated)', { exact: true }).fill('furniture');
  await page.getByLabel(/^description/).fill('A table created by the end-to-end tests; safe to delete.');
};

test('5. the true administrator adds a product, sees it listed, edits it (a second picture shows on its page) and deletes it', async ({ page }) => {
  const title = `E2E Table ${Date.now()}`;
  await login(page, trueAdmin.email, trueAdmin.password);
  await expect(page).toHaveURL(/\/admin$/);

  await page.goto('/admin/products/new');
  await fillProductForm(page, title, '123.45');
  await page.getByRole('button', { name: 'Save' }).click();
  await expectToast(page, 'Product created.');
  await expect(page).toHaveURL(/\/admin\/products$/);

  // The listing sorts by id, so the new product is on the last page
  await page.getByRole('button', { name: 'Sort by ID' }).click();
  const row = page.getByRole('row', { name: new RegExp(title) });
  await expect(row).toBeVisible();
  await expect(row).toContainText('$123.45');

  await row.getByRole('link', { name: 'Edit' }).click();
  await expect(page.getByLabel('title', { exact: true })).toHaveValue(title);
  const id = new URL(page.url()).pathname.split('/').pop();
  await page.getByLabel('price', { exact: true }).fill('150');
  await page.getByRole('button', { name: 'Add a picture' }).click();
  await page.getByLabel('Picture 2: address').fill('https://images.example.com/e2e-table-2.jpg');
  await page.getByLabel('Picture 2: what it shows').fill('The table top up close');
  await page.getByRole('button', { name: 'Save' }).click();
  await expectToast(page, 'Product updated.');

  await page.goto(`/products/${id}`);
  await expect(page.getByRole('heading', { level: 1, name: title })).toBeVisible();
  await expect(page.getByRole('button', { name: /^Show picture/ })).toHaveCount(2);
  await page.goto('/admin/products');
  await page.getByRole('button', { name: 'Sort by ID' }).click();
  await expect(page.getByRole('row', { name: new RegExp(title) })).toContainText('$150.00');

  await page.getByRole('button', { name: `Delete ${title}` }).click();
  await expect(page.getByRole('dialog')).toContainText(title);
  await page.getByRole('dialog').getByRole('button', { name: 'Delete' }).click();
  await expectToast(page, 'Product deleted.');
  // Deleting hides the product from the shop (it stays listed for the administrator as inactive)
  await expect(page.getByRole('row', { name: new RegExp(title) })).toContainText('Inactive');
});

test('6. the demo administrator may look but not change: a save is refused with 403', async ({ page }) => {
  await loginAsDemoAdmin(page);
  await expect(page).toHaveURL(/\/admin$/);
  await expect(page.getByText('Revenue & Orders (Daily)')).toBeVisible();

  await page.goto('/admin/products/new');
  await fillProductForm(page, 'Demo admin table', '99');
  const refused = page.waitForResponse((response) => response.url().includes('/api/v1/products') && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Save' }).click();

  expect((await refused).status()).toBe(403);
  await expectToast(page, 'You are not allowed to do this.');
  await expect(page).toHaveURL(/\/admin\/products\/new$/);
});

test('a customer cannot open the admin panel', async ({ page }) => {
  await page.goto('/login');
  await page.getByRole('button', { name: 'Demo User' }).click();
  await expectToast(page, 'Demo user logged in!');

  await page.goto('/admin');

  await expect(page).toHaveURL(/\/$/);
});

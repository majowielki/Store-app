import { expect, test, type Page } from '@playwright/test';
import { expectToast, login, loginAsDemoAdmin, trueAdmin } from './helpers';

const cover = 'http://localhost:10000/devstoreaccount1/product-images/Collection-WarmMinimal.webp';

const fillCollection = async (page: Page, title: string) => {
  await page.getByLabel('Title', { exact: true }).fill(title);
  await page.getByLabel('Summary', { exact: true }).fill('Two pieces for a reading corner, put together by the end-to-end tests.');
  await page.getByLabel('Cover picture', { exact: true }).fill(cover);
  await page.getByLabel('Text', { exact: true }).fill('## A chair and a lamp\n\nThat is all a corner needs.');
  await page.getByLabel('Products', { exact: true }).fill('tripod');
  await page.getByRole('button', { name: 'Add Tripod Floor Lamp' }).click();
  await page.getByLabel(/^Published/).check();
};

test('7. the true administrator publishes a collection and the shop shows it with its products', async ({ page }) => {
  const title = `E2E Corner ${Date.now()}`;
  const slug = title.toLowerCase().replace(/ /g, '-');
  await login(page, trueAdmin.email, trueAdmin.password);
  await expect(page).toHaveURL(/\/admin$/);

  await page.goto('/admin/content/collections/new');
  await fillCollection(page, title);
  // The address follows the title until it is typed by hand
  await expect(page.getByLabel('Address (slug)')).toHaveValue(slug);
  await page.getByRole('button', { name: 'Save' }).click();
  await expectToast(page, 'The collection is saved.');
  await expect(page.getByRole('row', { name: new RegExp(title) })).toContainText('Published');

  await page.goto(`/collections/${slug}`);
  await expect(page.getByRole('heading', { level: 1, name: title })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'A chair and a lamp' })).toBeVisible();
  await expect(page.getByRole('region', { name: `Products in ${title}` })).toContainText('Tripod Floor Lamp');

  await page.goto('/admin/content/collections');
  await page.getByRole('button', { name: `Delete ${title}` }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Delete' }).click();
  await expectToast(page, `${title} deleted.`);
  await page.goto(`/collections/${slug}`);
  await expect(page.getByRole('heading', { name: 'Collection not found' })).toBeVisible();
});

test('8. the demo administrator may open the content forms but a save is refused with 403', async ({ page }) => {
  await loginAsDemoAdmin(page);
  await expect(page).toHaveURL(/\/admin$/);

  await page.goto('/admin/content/collections/new');
  await fillCollection(page, 'Demo admin corner');
  const refused = page.waitForResponse((response) => response.url().includes('/api/v1/content/admin/collections') && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Save' }).click();

  expect((await refused).status()).toBe(403);
  await expectToast(page, 'You are not allowed to do this.');
  await expect(page).toHaveURL(/\/admin\/content\/collections\/new$/);
});

test('9. a point on a lookbook shows its product, and the whole look goes into the bag', async ({ page }) => {
  await page.goto('/looks/living-room');
  await expect(page.getByRole('heading', { level: 1, name: 'A warm living room' })).toBeVisible();

  const lamp = page.getByRole('button', { name: /^Paper Arc Floor Lamp/ });
  await lamp.focus();
  await page.keyboard.press('Enter');
  const card = page.getByRole('dialog', { name: 'Paper Arc Floor Lamp' });
  await expect(card).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(card).toBeHidden();

  await page.getByRole('button', { name: /^Add the whole look/ }).click();
  await expectToast(page, '7 pieces added to your bag.');
  await page.goto('/cart');
  await expect(page.getByText('Paper Arc Floor Lamp')).toBeVisible();
  await expect(page.getByText('Travertine Coffee Table')).toBeVisible();
});

import { expect, test } from '@playwright/test';
import { addToCart, checkout, expectToast, findProducts, login, register, trueAdmin, uniqueEmail } from './helpers';

/**
 * Reviews (ADR 012): a customer reviews a piece of a paid order, the review waits until the true
 * administrator approves it, and then the product page shows it to everyone.
 */
test('20. a customer reviews a paid piece, the true administrator approves it and the product page shows it', async ({ page, browser, request }) => {
  const [product] = await findProducts(request, { pageSize: '1' });
  // Several runs review the same product; the title tells this run's review apart (in base 36:
  // nine digits in a row would read as a phone number)
  const title = `Run ${Date.now().toString(36)}`;
  await register(page, uniqueEmail('review'));
  await addToCart(page, product.id);
  const orderId = await checkout(page);

  // The review service hears of the payment by an event: the product page offers the form once it has
  // (each try waits for the answer: a reload while the page renews the session would end it)
  const write = page.getByRole('button', { name: 'Write a review' });
  await expect(async () => {
    await page.goto(`/products/${product.id}`);
    await expect(write.or(page.getByText(/Reviews come from customers who bought/))).toBeVisible({ timeout: 10_000 });
    await expect(write).toBeVisible({ timeout: 1_000 });
  }).toPass({ timeout: 30_000 });

  await page.goto(`/orders/${orderId}`);
  await page.getByRole('button', { name: `Write a review: ${product.title}` }).click();
  const form = page.getByRole('form', { name: `Review of ${product.title}` });
  await form.getByRole('radio', { name: '5 stars' }).check();
  await form.getByLabel('Title (optional)').fill(title);
  await form.getByLabel('Review').fill('Solid, warm and exactly as pictured - it made the room.');
  await form.getByRole('button', { name: 'Send review' }).click();
  await expect(page.getByText(/Your review is awaiting moderation/)).toBeVisible();
  await page.getByRole('button', { name: 'Done' }).click();
  await expect(page.getByText('Awaiting moderation', { exact: true })).toBeVisible();

  // Only its author sees it until a person has read it
  await page.goto(`/products/${product.id}`);
  await expect(page.getByText(/Awaiting moderation - only you can see it/)).toBeVisible();

  const adminContext = await browser.newContext();
  const admin = await adminContext.newPage();
  await login(admin, trueAdmin.email, trueAdmin.password);
  await admin.goto('/admin/reviews');
  const queued = admin.getByRole('listitem').filter({ hasText: title });
  await expect(queued).toContainText(product.title);
  await queued.getByRole('button', { name: 'Approve the review by E2e T.' }).click();
  await expectToast(admin, '1 review published');
  await adminContext.close();

  await page.reload();
  await expect(page.getByRole('article', { name: 'Review by E2e T.' }).filter({ hasText: title })).toBeVisible();
  await expect(page.getByText(/Awaiting moderation - only you can see it/)).toHaveCount(0);
});

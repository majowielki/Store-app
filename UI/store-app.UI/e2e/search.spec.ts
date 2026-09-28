import { expect, test } from '@playwright/test';

/**
 * Search: the catalogue's full-text search corrects a mistyped word to the nearest word of the
 * catalogue, both in the search box and on the listing.
 */
test('21. a mistyped search is corrected: "sfoa" finds the sofas, in the search box and on the listing', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: /^Search/ }).first().click();
  const box = page.getByRole('searchbox', { name: 'Search products' });
  await box.fill('sfoa');

  await expect(page.getByText('Showing results for “sofa” — nothing matched “sfoa”.')).toBeVisible();
  await expect(page.getByRole('link', { name: /Sofa/ }).first()).toBeVisible();
  await box.press('Enter');

  await expect(page).toHaveURL(/\/products\?search=sofa$/);
  await expect(page.getByRole('heading', { level: 1, name: '“sofa”' })).toBeVisible();

  // Typed straight into the address, the listing says what it searched for instead
  await page.goto('/products?search=sfoa');
  await expect(page.getByRole('heading', { level: 1, name: '“sofa”' })).toBeVisible();
  await expect(page.getByText('Nothing matched “sfoa”, so these are the results for “sofa”.')).toBeVisible();
  await expect(page.getByRole('link', { name: /Sofa/ }).first()).toBeVisible();
});

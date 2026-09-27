import { expect, test } from '@playwright/test';

/** What the scenario reads from an <img> in the page (the e2e project has no DOM types). */
type Picture = { complete: boolean; naturalWidth: number; src: string };

// The home page shows every kind of content once, in this order, and nothing on it is broken
const sections = [
  'New arrivals, fresh from the workshop.',
  'A warm living room',
  'Start with the room, not the catalogue.',
  'Five workshops, one standard.',
  'Ideas for living well.',
  '20% off your first order.',
];

test('10. the home page shows each section once, with its content and every picture loaded', async ({ page }) => {
  const errors: string[] = [];
  page.on('console', (message) => message.type() === 'error' && errors.push(message.text()));
  page.on('pageerror', (error) => errors.push(error.message));

  await page.goto('/');
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(/Make every\s*room feel\s*like home\./);

  const main = page.getByRole('main');
  await expect(main.getByRole('region')).toHaveCount(sections.length);
  for (const [index, name] of sections.entries()) {
    await expect(main.getByRole('region').nth(index)).toHaveAccessibleName(name);
  }

  const arrivals = page.getByRole('region', { name: sections[0] });
  await expect(arrivals.getByRole('link', { name: /^All \d+ new pieces/ })).toBeVisible();
  await expect(arrivals.locator('a[href^="/products/"]')).toHaveCount(5);

  const look = page.getByRole('region', { name: sections[1] });
  await expect(look.getByRole('button', { name: /^Add the whole look/ })).toBeEnabled();
  await look.getByRole('button', { name: /^Paper Arc Floor Lamp/ }).click();
  await expect(look.getByRole('dialog', { name: 'Paper Arc Floor Lamp' })).toBeVisible();
  await page.keyboard.press('Escape');

  await expect(page.getByRole('region', { name: sections[2] }).locator('a[href^="/products?"]')).toHaveCount(8);
  await expect(page.getByRole('region', { name: sections[3] }).locator('a[href^="/makers/"]')).toHaveCount(5);
  await expect(page.getByRole('region', { name: sections[4] }).locator('a[href^="/journal/"]')).toHaveCount(3);
  await expect(page.getByRole('region', { name: sections[5] }).getByRole('link', { name: /Create an account/ })).toHaveAttribute('href', '/register');

  // Scroll to the end so every lazy picture loads, then check that none of them failed
  const height = await page.locator('body').evaluate((body) => body.scrollHeight);
  for (let y = 0; y < height; y += 600) {
    await page.mouse.wheel(0, 600);
    await page.waitForTimeout(50);
  }
  const pictures = page.locator('main img');
  await expect.poll(() => pictures.evaluateAll((images) => images.filter((image) => !(image as Picture).complete).length)).toBe(0);
  const broken = await pictures.evaluateAll((images) =>
    images.filter((image) => (image as Picture).naturalWidth === 0).map((image) => (image as Picture).src),
  );
  expect(broken).toEqual([]);
  expect(errors).toEqual([]);
});

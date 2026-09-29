import { readFileSync } from 'node:fs';
import { expect, test } from '@playwright/test';

interface IndexEntry {
  id: string;
  title: string;
  name: string;
  type: 'story' | 'docs';
  tags?: string[];
}

/** Every story of the built Storybook, from the index it writes next to its pages. */
const stories = Object.values((JSON.parse(readFileSync('storybook-static/index.json', 'utf8')) as { entries: Record<string, IndexEntry> }).entries).filter(
  (entry) => entry.type === 'story',
);

/** The day the screenshots are taken on, whatever the date: the footer's year, the delivery estimates. */
const TODAY = new Date('2026-09-29T10:00:00+02:00');

for (const story of stories) {
  test(`${story.title} › ${story.name}`, async ({ page }) => {
    await page.clock.setFixedTime(TODAY);
    await page.goto(`/iframe.html?id=${story.id}&viewMode=story`);
    // Storybook marks the page once the story has rendered, or once it failed to
    await expect(page.locator('body.sb-show-main, body.sb-show-errordisplay')).toBeAttached();
    await expect(page.locator('body'), 'the story renders without an error').toHaveClass(/sb-show-main/);
    // The API answers (MSW, in the page) and the pictures, then the fonts they are drawn with
    await page.waitForLoadState('networkidle');
    await page.waitForFunction(() => Array.from(document.images).every((image) => image.complete));
    await page.evaluate(() => document.fonts.ready);

    // The story's own area: the component with the padding around it, the full page for a fullscreen one
    await expect(page.locator('#storybook-root')).toHaveScreenshot(`${story.id}.png`);
  });
}

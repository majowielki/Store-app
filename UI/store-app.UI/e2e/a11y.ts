import AxeBuilder from '@axe-core/playwright';
import { expect, type Locator, type Page } from '@playwright/test';

/** The WCAG levels the shop keeps to (2.2 AA) and axe's own best practices on top of them. */
const standards = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'];

/**
 * Rules axe still marks experimental, switched on: a control's name must contain the words it
 * shows (speech users say what they see), which Lighthouse checks too.
 */
const experimental = { 'label-content-name-mismatch': { enabled: true } };

/**
 * Waits for the entrance animations (text fading and rising in) to end: a half-faded line fails the
 * contrast check although the page will not look like that. Endless ones, the ticker, keep running.
 */
const animationsSettled = (page: Page) =>
  page.waitForFunction(() =>
    document.getAnimations().every((animation) => animation.playState !== 'running' || animation.effect?.getComputedTiming().endTime === Infinity),
  );

/** Findings of these weights fail a scenario (the bar the plan sets); moderate and minor ones do not. */
const blocking = new Set(['serious', 'critical']);

/**
 * Scans the page as it stands with axe and fails on a serious or critical finding. The message
 * names every rule broken, its weight and the elements that break it, so the fix can start from it.
 */
export const expectNoSeriousViolations = async (page: Page, where: string) => {
  await animationsSettled(page);
  const { violations } = await new AxeBuilder({ page }).withTags(standards).options({ rules: experimental }).analyze();
  const failing = violations.filter((violation) => blocking.has(violation.impact ?? ''));
  const report = failing.map((violation) => `${violation.impact} ${violation.id}: ${violation.help}\n${violation.nodes.map((node) => `  ${node.target.join(' ')}`).join('\n')}`);
  expect(report, `accessibility of ${where}`).toEqual([]);
};

/** How far the Tab key may have to go before it reaches an element (the header comes first). */
const MAX_TABS = 80;

/**
 * Presses Tab until the element has the focus, the way a keyboard user gets to it, and fails if it
 * never does. The element must be reachable in the page's tab order, not only focusable by script.
 */
export const tabTo = async (page: Page, target: Locator) => {
  await expect(target).toBeVisible();
  for (let presses = 0; presses < MAX_TABS; presses++) {
    await page.keyboard.press('Tab');
    if (await target.evaluate((element) => element === document.activeElement)) return;
  }
  throw new Error(`Tab never reached ${target}`);
};

// Runs the visual tests in the Playwright image of the installed version, where CI runs them, so
// the screenshots are drawn with the same fonts: node scripts/visual-in-docker.mjs [playwright args],
// e.g. --update-snapshots after an intended change. Needs Docker and a built Storybook.
import { spawnSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const ui = resolve(import.meta.dirname, '..');
if (!existsSync(resolve(ui, 'storybook-static/index.json'))) {
  console.error('No built Storybook: run "npm run storybook:build" first.');
  process.exit(1);
}

const { version } = JSON.parse(readFileSync(resolve(ui, 'node_modules/@playwright/test/package.json'), 'utf8'));
const image = `mcr.microsoft.com/playwright:v${version}-noble`;
// CI switches the reporters and forbids test.only inside the container too
const ci = process.env.CI ? ['-e', 'CI'] : [];
const args = ['run', '--rm', '--ipc=host', ...ci, '-v', `${ui}:/ui`, '-w', '/ui', image, 'npx', 'playwright', 'test', '-c', 'playwright.visual.config.ts', ...process.argv.slice(2)];

console.log(`docker ${args.join(' ')}`);
const run = spawnSync('docker', args, { stdio: 'inherit', env: { ...process.env, MSYS_NO_PATHCONV: '1' } });
process.exit(run.status ?? 1);

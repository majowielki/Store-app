// Fails the build when the main bundle - the script index.html starts the app with - outgrows its
// budget. Everything else is a route or a shared chunk loaded when a page needs it (src/App.tsx).
//
//   node scripts/check-bundle-size.mjs      after "npm run build"; CI runs it in the frontend job
import { readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';

/** The main bundle's budget, uncompressed. */
const MAIN_BUNDLE_BUDGET_KB = 500;

const dist = join(import.meta.dirname, '..', 'dist');
const html = readFileSync(join(dist, 'index.html'), 'utf8');
const entry = html.match(/<script type="module"[^>]*src="\/([^"]+\.js)"/)?.[1];
if (!entry) {
  console.error('No module script in dist/index.html - run "npm run build" first.');
  process.exit(1);
}

const sizeKb = Math.round(statSync(join(dist, entry)).size / 1024);
const preloaded = [...html.matchAll(/<link rel="modulepreload"[^>]*href="\/([^"]+\.js)"/g)].map((match) => match[1]);
const firstScreenKb = Math.round([entry, ...preloaded].reduce((total, file) => total + statSync(join(dist, file)).size, 0) / 1024);

console.log(`main bundle ${entry}: ${sizeKb} kB of ${MAIN_BUNDLE_BUDGET_KB} kB; with the ${preloaded.length} chunks it preloads: ${firstScreenKb} kB`);
if (sizeKb > MAIN_BUNDLE_BUDGET_KB) {
  console.error(`The main bundle is over its budget by ${sizeKb - MAIN_BUNDLE_BUDGET_KB} kB: load the new code with its page (a lazy route) instead.`);
  process.exit(1);
}

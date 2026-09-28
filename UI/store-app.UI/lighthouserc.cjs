// Lighthouse CI (the "Lighthouse" job of .github/workflows/e2e.yml): the shop's main pages on a
// desktop against the stack from docker compose, three runs each, and the budget they must keep.
//
//   npx @lhci/cli@0.15.1 autorun       with the stack up on LHCI_BASE_URL (default localhost:8081)
//
// LHCI_PRODUCT_PATH names a product page of that stack (the ids differ between databases).
const base = process.env.LHCI_BASE_URL ?? 'http://localhost:8081';
const product = process.env.LHCI_PRODUCT_PATH ?? '/products/1';

module.exports = {
  ci: {
    collect: {
      url: ['/', '/products', product, '/journal/caring-for-oak', '/about'].map((path) => `${base}${path}`),
      numberOfRuns: 3,
      settings: {
        preset: 'desktop',
        // Chrome in a container or on a CI runner: no sandbox, and /dev/shm is small there
        chromeFlags: '--no-sandbox --disable-dev-shm-usage',
      },
    },
    assert: {
      // The median of the three runs is judged
      aggregationMethod: 'median-run',
      assertions: {
        'categories:performance': ['error', { minScore: 0.9 }],
        'categories:seo': ['error', { minScore: 0.95 }],
        'categories:accessibility': ['error', { minScore: 0.95 }],
        'categories:best-practices': ['error', { minScore: 0.95 }],
        'largest-contentful-paint': ['error', { maxNumericValue: 2500 }],
        'cumulative-layout-shift': ['error', { maxNumericValue: 0.1 }],
        'total-blocking-time': ['error', { maxNumericValue: 200 }],
        // The budget: what a page may download, compressed, the pictures of a whole listing included
        'resource-summary:script:size': ['error', { maxNumericValue: 400 * 1024 }],
        'resource-summary:stylesheet:size': ['error', { maxNumericValue: 40 * 1024 }],
        'resource-summary:font:size': ['error', { maxNumericValue: 250 * 1024 }],
        'resource-summary:total:size': ['error', { maxNumericValue: 1500 * 1024 }],
      },
    },
    upload: {
      target: 'filesystem',
      outputDir: './lighthouse-report',
    },
  },
};

// Generates src/api/schema/*.ts from the services' OpenAPI documents (docs/api/openapi), which
// Scripts/Export-OpenApi.ps1 writes from the built services. With --check the files are
// generated to memory and compared with the committed ones instead, for CI.
import { execFileSync } from 'node:child_process';
import { readFileSync, existsSync, mkdirSync, writeFileSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const documents = resolve(here, '../../../docs/api/openapi');
const output = resolve(here, '../src/api/schema');
const check = process.argv.includes('--check');
const names = ['identity', 'catalog', 'cart', 'orders', 'audit'];

mkdirSync(output, { recursive: true });
const stale = [];
for (const name of names) {
  const source = resolve(documents, `${name}.json`);
  const target = resolve(output, `${name}.ts`);
  const generated = execFileSync('npx', ['openapi-typescript', source], { cwd: resolve(here, '..'), encoding: 'utf8', shell: true });
  if (check) {
    if (!existsSync(target) || readFileSync(target, 'utf8') !== generated) stale.push(name);
  } else {
    writeFileSync(target, generated);
    console.log(`generated ${name} -> ${target}`);
  }
}

if (check) {
  if (stale.length > 0) {
    console.error(`API types are out of date: ${stale.join(', ')}. Run "npm run api:generate" and commit src/api/schema.`);
    process.exit(1);
  }
  console.log('API types are up to date');
}

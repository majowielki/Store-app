// Serves a built folder (storybook-static) over HTTP for the visual tests:
// node scripts/serve-static.mjs <folder> <port>. Only what those files need; not for production.
import { createReadStream, statSync } from 'node:fs';
import { createServer } from 'node:http';
import { extname, join, normalize, resolve } from 'node:path';

const [folder = 'storybook-static', port = '6006'] = process.argv.slice(2);
const root = resolve(folder);

const types = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.webp': 'image/webp',
  '.woff2': 'font/woff2',
  '.ico': 'image/x-icon',
};

/** The file a request path names inside the folder, or null for anything outside it or missing. */
const fileFor = (url) => {
  const path = decodeURIComponent(new URL(url, 'http://localhost').pathname);
  const file = normalize(join(root, path.endsWith('/') ? `${path}index.html` : path));
  if (!file.startsWith(root)) return null;
  try {
    return statSync(file).isFile() ? file : null;
  } catch {
    return null;
  }
};

createServer((request, response) => {
  const file = fileFor(request.url ?? '/');
  if (!file) {
    response.writeHead(404).end();
    return;
  }
  response.writeHead(200, { 'Content-Type': types[extname(file)] ?? 'application/octet-stream' });
  createReadStream(file).pipe(response);
}).listen(Number(port), () => console.log(`Serving ${root} on http://localhost:${port}`));

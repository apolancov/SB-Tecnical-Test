import { createReadStream, statSync } from 'node:fs';
import { createServer } from 'node:http';
import { extname, join, resolve, sep } from 'node:path';

const ROOT = resolve(process.env.STATIC_ROOT ?? '/app/out');
const PORT = Number(process.env.PORT ?? 3000);
const HOST = process.env.HOST ?? '0.0.0.0';

const MIME_TYPES = new Map([
  ['.html', 'text/html; charset=utf-8'],
  ['.css', 'text/css; charset=utf-8'],
  ['.js', 'application/javascript; charset=utf-8'],
  ['.mjs', 'application/javascript; charset=utf-8'],
  ['.json', 'application/json; charset=utf-8'],
  ['.map', 'application/json; charset=utf-8'],
  ['.svg', 'image/svg+xml'],
  ['.png', 'image/png'],
  ['.jpg', 'image/jpeg'],
  ['.jpeg', 'image/jpeg'],
  ['.gif', 'image/gif'],
  ['.webp', 'image/webp'],
  ['.ico', 'image/x-icon'],
  ['.txt', 'text/plain; charset=utf-8'],
  ['.woff', 'font/woff'],
  ['.woff2', 'font/woff2'],
  ['.xml', 'application/xml; charset=utf-8'],
]);

function mimeTypeFor(filePath) {
  return MIME_TYPES.get(extname(filePath).toLowerCase()) ?? 'application/octet-stream';
}

function cacheControlFor(filePath, urlPath) {
  if (urlPath.includes('/_next/static/')) {
    return 'public, max-age=31536000, immutable';
  }
  if (filePath.endsWith('.html')) {
    return 'no-store';
  }
  return 'public, max-age=3600';
}

function resolveTargetFile(rawUrl) {
  const withoutQuery = rawUrl.split('?')[0].split('#')[0];
  let decoded;
  try {
    decoded = decodeURIComponent(withoutQuery);
  } catch {
    return null;
  }

  if (decoded.includes('\0')) {
    return null;
  }

  const requested = resolve(ROOT, '.' + decoded);

  if (requested !== ROOT && !requested.startsWith(ROOT + sep)) {
    return null;
  }

  try {
    const stats = statSync(requested);
    if (stats.isFile()) {
      return { filePath: requested, servedUrlPath: decoded };
    }
    if (stats.isDirectory()) {
      const indexPath = join(requested, 'index.html');
      const indexStats = statSync(indexPath);
      if (indexStats.isFile()) {
        return { filePath: indexPath, servedUrlPath: decoded };
      }
    }
  } catch {
  }
  return null;
}

const server = createServer((req, res) => {
  const result = resolveTargetFile(req.url ?? '/');
  if (result === null) {
    res.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
    res.end('Not Found');
    return;
  }

  res.writeHead(200, {
    'Content-Type': mimeTypeFor(result.filePath),
    'Cache-Control': cacheControlFor(result.filePath, result.servedUrlPath),
  });
  createReadStream(result.filePath).pipe(res);
});

server.listen(PORT, HOST, () => {
  console.log(`Static server listening on http://${HOST}:${PORT} (root: ${ROOT})`);
});

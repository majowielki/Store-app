#!/bin/sh
# Runs from /docker-entrypoint.d before nginx starts: turns environment variables into the
# runtime configuration the SPA reads from /config.js. The default keeps the same-origin setup
# (API under /api/v1 on this host, proxied to the gateway).
set -eu

API_BASE_URL="${API_BASE_URL:-/api/v1}"

cat > /usr/share/nginx/html/config.js <<CONFIG
window.__APP_CONFIG__ = {
  apiBaseUrl: "${API_BASE_URL}"
};
CONFIG

echo "app config: apiBaseUrl=${API_BASE_URL}"

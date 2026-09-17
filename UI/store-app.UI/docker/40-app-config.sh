#!/bin/sh
# Runs from /docker-entrypoint.d before nginx starts: turns environment variables into the
# runtime configuration the SPA reads from /config.js. Defaults keep the same-origin setup
# (API under /api/v1 on this host, proxied to the gateway).
set -eu

API_BASE_URL="${API_BASE_URL:-/api/v1}"
USE_AUTH_ME="${USE_AUTH_ME:-true}"

cat > /usr/share/nginx/html/config.js <<CONFIG
window.__APP_CONFIG__ = {
  apiBaseUrl: "${API_BASE_URL}",
  useAuthMe: ${USE_AUTH_ME}
};
CONFIG

echo "app config: apiBaseUrl=${API_BASE_URL} useAuthMe=${USE_AUTH_ME}"

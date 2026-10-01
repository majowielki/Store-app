#!/bin/sh
# Runs from /docker-entrypoint.d before nginx starts: turns environment variables into the
# runtime configuration the SPA reads from /config.js. The default keeps the same-origin setup
# (API under /api/v1 on this host, proxied to the gateway). SHOP_URL comes trimmed from
# 16-shop-url.envsh; empty, the app uses the address it was opened at.
set -eu

API_BASE_URL="${API_BASE_URL:-/api/v1}"
SHOP_URL="${SHOP_URL:-}"
REQUEST_TIMEOUT_MS="${REQUEST_TIMEOUT_MS:-15000}"
RENEW_BEFORE_EXPIRY_MS="${RENEW_BEFORE_EXPIRY_MS:-30000}"
DASHBOARD_DAYS="${DASHBOARD_DAYS:-30}"

check_number() {
  case "$2" in ''|*[!0-9]*|0[0-9]*) echo "$1 must be an integer" >&2; exit 1 ;; esac
  if [ "${#2}" -gt 6 ] || [ "$2" -lt "$3" ] || [ "$2" -gt "$4" ]; then
    echo "$1 must be between $3 and $4" >&2
    exit 1
  fi
}
check_number REQUEST_TIMEOUT_MS "$REQUEST_TIMEOUT_MS" 1000 120000
check_number RENEW_BEFORE_EXPIRY_MS "$RENEW_BEFORE_EXPIRY_MS" 0 300000
check_number DASHBOARD_DAYS "$DASHBOARD_DAYS" 1 3650


cat > /usr/share/nginx/html/config.js <<CONFIG
window.__APP_CONFIG__ = {
  apiBaseUrl: "${API_BASE_URL}",
  shopUrl: "${SHOP_URL}",
  requestTimeoutMs: ${REQUEST_TIMEOUT_MS},
  renewBeforeExpiryMs: ${RENEW_BEFORE_EXPIRY_MS},
  dashboardDays: ${DASHBOARD_DAYS}
};
CONFIG

echo "app config: apiBaseUrl=${API_BASE_URL} shopUrl=${SHOP_URL}"

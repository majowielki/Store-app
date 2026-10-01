/** Values the container injects at startup through /config.js (see docker/40-app-config.sh). */
interface AppRuntimeConfig {
  apiBaseUrl?: string;
  requestTimeoutMs?: number;
  dashboardDays?: number;
  renewBeforeExpiryMs?: number;
  /** The shop's public address (SHOP_URL), for canonical links; empty when the container was not told. */
  shopUrl?: string;
}

declare global {
  interface Window {
    __APP_CONFIG__?: AppRuntimeConfig;
  }
}

const runtime: AppRuntimeConfig = (typeof window !== 'undefined' && window.__APP_CONFIG__) || {};

/**
 * Base URL of the API. Precedence: runtime config from the container, then the Vite build-time
 * variable, then the same-origin "/api/v1" that nginx (in the container) or the Vite dev server
 * proxies to the gateway. Same-origin is the normal case: the refresh cookie needs no CORS then.
 */
export const apiBaseUrl: string = runtime.apiBaseUrl || import.meta.env.VITE_API_BASE_URL || '/api/v1';

/**
 * The shop's public address, without a trailing slash: the one the container was given, else the
 * address the app was opened at. Canonical links and the addresses in structured data use it, so a
 * page reached through a second host name still points search engines at the first.
 */
export const shopUrl: string = (runtime.shopUrl || (typeof window !== 'undefined' ? window.location.origin : '')).replace(/\/$/, '');

export {};

const boundedSetting = (value: number | undefined, fallback: number, min: number, max: number) =>
  value !== undefined && Number.isInteger(value) && value >= min && value <= max ? value : fallback;

export const httpOptions = {
  requestTimeoutMs: boundedSetting(runtime.requestTimeoutMs, 15_000, 1_000, 120_000),
  renewBeforeExpiryMs: boundedSetting(runtime.renewBeforeExpiryMs, 30_000, 0, 300_000),
};

export const dashboardDays = boundedSetting(runtime.dashboardDays, 30, 1, 3650);

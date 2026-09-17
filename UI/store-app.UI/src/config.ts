/** Values the container injects at startup through /config.js (see docker/40-app-config.sh). */
interface AppRuntimeConfig {
  apiBaseUrl?: string;
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

export {};

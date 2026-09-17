/** Values the container injects at startup through /config.js (see docker/40-app-config.sh). */
interface AppRuntimeConfig {
  apiBaseUrl?: string;
  useAuthMe?: boolean;
}

declare global {
  interface Window {
    __APP_CONFIG__?: AppRuntimeConfig;
  }
}

const runtime: AppRuntimeConfig = (typeof window !== 'undefined' && window.__APP_CONFIG__) || {};

/**
 * Base URL of the API. Precedence: runtime config from the container, then the Vite build-time
 * variable (local development), then the same-origin "/api/v1" that nginx proxies to the gateway.
 */
export const apiBaseUrl: string = runtime.apiBaseUrl || import.meta.env.VITE_API_BASE_URL || '/api/v1';

/** Whether to validate the stored token against GET /auth/me on start-up. */
export const useAuthMe: boolean = runtime.useAuthMe ?? import.meta.env.VITE_USE_AUTH_ME === 'true';

export {};

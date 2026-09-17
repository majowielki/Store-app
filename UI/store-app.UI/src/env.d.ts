/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the API when the app is served from another origin than the gateway; see config.ts. */
  readonly VITE_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

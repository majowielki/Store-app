import axios, { AxiosRequestConfig } from "axios";

// Use environment variable configured in .env files with a safe fallback
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api";

export const customFetch = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000, // 10 second timeout
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor for adding auth token
customFetch.interceptors.request.use(
  (config) => {
    // Add auth token if available
    const token = localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor for handling common errors
customFetch.interceptors.response.use(
  (response) => response,
  (error) => {
    // Handle 401 Unauthorized
    if (error.response?.status === 401) {
      const isMeEndpoint = typeof error.config?.url === 'string' && /\/auth\/me\b/.test(error.config.url);
      const expired = error.response.headers?.['token-expired'] === 'true' ||
                      error.response.headers?.['Token-Expired'] === 'true';
      const wwwAuth: string | undefined = error.response.headers?.['www-authenticate'];
      const looksInvalid = typeof wwwAuth === 'string' && /invalid_token|invalid signature|signature|malformed|audience|issuer/i.test(wwwAuth);
      if (expired) {
        // Only clear on true expiry to avoid wiping a fresh token due to race conditions
        localStorage.removeItem('authToken');
        sessionStorage.removeItem('authToken');
        console.warn('Authentication token expired');
      } else if (isMeEndpoint) {
        // If the current-user endpoint says 401, our stored token isn't usable. Clear it to recover.
        localStorage.removeItem('authToken');
        sessionStorage.removeItem('authToken');
        console.warn('Unauthorized on /auth/me; cleared stored token');
      } else if (looksInvalid) {
        // Token is not acceptable by server (bad signature/issuer/audience). Clear to recover.
        localStorage.removeItem('authToken');
        sessionStorage.removeItem('authToken');
        console.warn('Authentication token invalid; clearing stored token');
      } else {
        console.warn('Unauthorized request (token present but not accepted). Keeping token for retry.');
      }
    }

    // Handle 400 Bad Request cases
    if (error.response?.status === 400) {
      try {
        const data = error.response.data;
        const message: string | undefined = typeof data === 'string' ? data : data?.message;
        const isMeEndpoint = typeof error.config?.url === 'string' && /\/auth\/me\b/.test(error.config.url);
        if (isMeEndpoint && message && /user not found/i.test(message)) {
          localStorage.removeItem('authToken');
          sessionStorage.removeItem('authToken');
          console.warn('Cleared stale token after 400 User not found on /auth/me');
        }

        // If a GET public endpoint like /products fails with 400 and Authorization header might be malformed,
        // retry once without Authorization header to avoid blocking anonymous users
        type MarkedConfig = AxiosRequestConfig & { _retriedNoAuth?: boolean };
        const cfg = (error.config || {}) as MarkedConfig;
        const method = (cfg.method || 'get').toLowerCase();
        const isGet = method === 'get';
        const alreadyRetried = cfg._retriedNoAuth === true;
        const isPublic = typeof cfg.url === 'string' && /\/products\b/.test(cfg.url);
        if (isGet && isPublic && !alreadyRetried) {
          cfg._retriedNoAuth = true;
          const newHeaders: Record<string, string> = {};
          const curHeaders = cfg.headers as Record<string, string> | undefined;
          if (curHeaders) {
            for (const [k, v] of Object.entries(curHeaders)) {
              if (k.toLowerCase() !== 'authorization') newHeaders[k] = v as unknown as string;
            }
          }
          const newCfg: AxiosRequestConfig = { ...cfg, headers: newHeaders };
          return customFetch.request(newCfg);
        }
      } catch {
        // no-op
      }
    }
    
    // Handle network errors
    if (!error.response) {
      console.error('Network error:', error.message);
    }
    
    return Promise.reject(error);
  }
);

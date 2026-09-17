import { toast } from '@/hooks/use-toast';
import { extractApiErrorMessage, getStatus } from './errorHandling';
import { endSession, getAccessToken, setAccessToken } from './session';
import axios, { type InternalAxiosRequestConfig } from 'axios';
import { apiBaseUrl } from '@/config';
import type { AuthResponse } from '@/api/types';

export const customFetch = axios.create({
  baseURL: apiBaseUrl,
  timeout: 10000, // 10 second timeout
  headers: {
    'Content-Type': 'application/json',
  },
  // The refresh cookie must travel with the auth requests when the API is on another origin
  withCredentials: true,
});

/** Requests that establish or end a session; a 401 from them is an answer, not an expired token. */
const isSessionEndpoint = (url: unknown) =>
  typeof url === 'string' && /\/auth\/(login|register|demo-login|demo-admin-login|refresh|logout)\b/.test(url);

const isMeEndpoint = (url: unknown) => typeof url === 'string' && /\/auth\/me\b/.test(url);

let refreshing: Promise<string | null> | null = null;

/**
 * Trades the refresh cookie for a new access token. Concurrent callers share one request, so a
 * burst of expired requests refreshes once; a refusal ends the session for the whole app.
 */
export const refreshAccessToken = (): Promise<string | null> => {
  refreshing ??= axios
    .post<AuthResponse>(`${apiBaseUrl}/auth/refresh`, undefined, { withCredentials: true, timeout: 10000 })
    .then((response) => {
      setAccessToken(response.data.accessToken);
      return response.data.accessToken;
    })
    .catch(() => {
      endSession();
      return null;
    })
    .finally(() => {
      refreshing = null;
    });
  return refreshing;
};

customFetch.interceptors.request.use(
  (config) => {
    const token = getAccessToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

type RetriedConfig = InternalAxiosRequestConfig & { _retriedAfterRefresh?: boolean };

// Errors arrive as problem responses (application/problem+json): the status code says what
// happened, "detail" says why. A 401 means the access token ran out: the request is repeated
// once with a fresh token, and only when that is refused does the user hear about it.
customFetch.interceptors.response.use(
  (response) => response,
  async (error) => {
    const status = getStatus(error);
    const config = (error.config ?? {}) as RetriedConfig;

    if (status === 401 && !isSessionEndpoint(config.url) && !config._retriedAfterRefresh) {
      config._retriedAfterRefresh = true;
      const token = await refreshAccessToken();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
        return customFetch.request(config);
      }
    }

    if (status === 404 && isMeEndpoint(config.url)) {
      // The account behind the session no longer exists
      endSession();
    }

    // A visitor whose session simply ran out is not an error worth a toast
    const silent = status === 401 && (isMeEndpoint(config.url) || config._retriedAfterRefresh);
    if (!silent) {
      toast({
        title: 'API Error',
        description: extractApiErrorMessage(error),
        variant: 'destructive',
      });
    }

    if (!error.response) {
      console.error('Network error:', error.message);
    }

    return Promise.reject(error);
  }
);

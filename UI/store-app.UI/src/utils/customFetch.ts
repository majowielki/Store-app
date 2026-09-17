import { toast } from '@/hooks/use-toast';
import { extractApiErrorMessage, getStatus } from './errorHandling';
import axios from 'axios';
import { apiBaseUrl } from '@/config';

export const customFetch = axios.create({
  baseURL: apiBaseUrl,
  timeout: 10000, // 10 second timeout
  headers: {
    'Content-Type': 'application/json',
  },
});

const readToken = () => localStorage.getItem('authToken') || sessionStorage.getItem('authToken');

const clearToken = (reason: string) => {
  localStorage.removeItem('authToken');
  sessionStorage.removeItem('authToken');
  console.warn(`Cleared stored token: ${reason}`);
};

const isMeEndpoint = (url: unknown) => typeof url === 'string' && /\/auth\/me\b/.test(url);

// Request interceptor for adding auth token
customFetch.interceptors.request.use(
  (config) => {
    const token = readToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Errors arrive as problem responses (application/problem+json): the status code says what
// happened, "detail" says why. The interceptor only decides what to do with the stored token
// and shows the message; callers get the rejected promise.
customFetch.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = getStatus(error);
    const me = isMeEndpoint(error.config?.url);

    if (status === 401) {
      const expired = error.response?.headers?.['token-expired'] === 'true';
      if (expired) {
        clearToken('the token expired');
      } else if (me) {
        // The current-user endpoint refused the token: it is not usable, forget it
        clearToken('the token was rejected');
      }
    } else if (status === 404 && me) {
      // The account behind the token no longer exists
      clearToken('the account no longer exists');
    }

    // An anonymous visitor asking /auth/me is not an error worth a toast
    const silent = status === 401 && me && !readToken();
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

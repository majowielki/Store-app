import type { BaseQueryFn } from '@reduxjs/toolkit/query';
import { apiBaseUrl } from '@/config';
import { describeProblem, type ApiError } from './problem';
import { endSession, getAccessToken, setAccessToken } from './session';
import type { AuthResponse, ProblemDetails } from './types';

/** One request as the endpoints describe it; a bare string is a GET of that path. */
export interface ApiRequest {
  url: string;
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
  /** Query string; undefined, null and empty values are left out. */
  params?: Record<string, string | number | boolean | null | undefined>;
  headers?: Record<string, string>;
}

/** Marks an endpoint whose failures the user should not be told about with a toast. */
export interface ApiExtraOptions {
  silent?: boolean;
}

/** Travels with every result (action.meta.baseQueryMeta), so the error middleware knows what to keep quiet about. */
export interface ApiResultMeta {
  silent: boolean;
}

const REQUEST_TIMEOUT_MS = 15_000;

/** Requests that establish or end a session; a 401 from them is an answer, not an expired token. */
const isSessionRequest = (url: string) => /^\/auth\/(login|register|demo-login|demo-admin-login|logout)$/.test(url);

const buildUrl = (url: string, params?: ApiRequest['params']): string => {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params ?? {})) {
    if (value === undefined || value === null || value === '') continue;
    query.set(key, String(value));
  }
  const suffix = query.toString();
  return `${apiBaseUrl}${url}${suffix ? `?${suffix}` : ''}`;
};

/** Aborts when the caller aborts (RTK Query, on unsubscribe) or when the request takes too long. */
const withTimeout = (signal: AbortSignal): { signal: AbortSignal; clear: () => void } => {
  const controller = new AbortController();
  const timer = window.setTimeout(() => controller.abort(new DOMException('Timed out', 'TimeoutError')), REQUEST_TIMEOUT_MS);
  const forward = () => controller.abort(signal.reason);
  if (signal.aborted) forward();
  else signal.addEventListener('abort', forward, { once: true });
  return {
    signal: controller.signal,
    clear: () => {
      window.clearTimeout(timer);
      signal.removeEventListener('abort', forward);
    },
  };
};

// The body is always read to the end, so the browser does not report the request as aborted
// when the caller (RTK Query) drops it right after the answer
const readBody = async (response: Response): Promise<unknown> => {
  const text = await response.text();
  return text === '' ? undefined : JSON.parse(text);
};

const asProblem = (body: unknown): ProblemDetails | undefined =>
  body && typeof body === 'object' && ('title' in body || 'detail' in body || 'status' in body)
    ? (body as ProblemDetails)
    : undefined;

let refreshing: Promise<string | null> | null = null;

/**
 * Trades the refresh cookie for a new access token. Concurrent callers share one request, so
 * a burst of expired requests refreshes once; a refusal ends the session for the whole app.
 */
export const refreshAccessToken = (): Promise<string | null> => {
  refreshing ??= fetch(`${apiBaseUrl}/auth/refresh`, { method: 'POST', credentials: 'include' })
    .then(async (response) => {
      if (!response.ok) throw new Error(`refresh refused: ${response.status}`);
      const session = (await response.json()) as AuthResponse;
      setAccessToken(session.accessToken);
      return session.accessToken;
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

type Attempt = { data: unknown } | { error: ApiError };

const send = async (request: ApiRequest, signal: AbortSignal): Promise<Attempt> => {
  const headers = new Headers(request.headers);
  headers.set('Accept', 'application/json, application/problem+json');
  if (request.body !== undefined) headers.set('Content-Type', 'application/json');
  const token = getAccessToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  const timeout = withTimeout(signal);
  let response: Response;
  try {
    response = await fetch(buildUrl(request.url, request.params), {
      method: request.method ?? 'GET',
      headers,
      body: request.body === undefined ? undefined : JSON.stringify(request.body),
      // The refresh cookie must travel with the auth requests when the API is on another origin
      credentials: 'include',
      signal: timeout.signal,
    });
  } catch (error) {
    const timedOut = error instanceof DOMException && error.name === 'TimeoutError';
    const status = timedOut ? 'TIMEOUT_ERROR' : 'FETCH_ERROR';
    return { error: { status, message: describeProblem(status) } };
  } finally {
    timeout.clear();
  }

  let body: unknown;
  try {
    body = await readBody(response);
  } catch {
    return { error: { status: 'PARSING_ERROR', message: describeProblem('PARSING_ERROR') } };
  }
  if (response.ok) return { data: body };
  const problem = asProblem(body);
  return { error: { status: response.status, problem, message: describeProblem(response.status, problem) } };
};

/**
 * Every endpoint goes through here: the bearer token is attached, failures come back as an
 * ApiError built from the problem response, and a 401 on an ordinary request means the access
 * token ran out - the request is repeated once with a fresh one, and only when the refresh is
 * refused does the caller see the 401 (marked as the end of the session).
 */
export const baseQuery: BaseQueryFn<ApiRequest | string, unknown, ApiError, ApiExtraOptions, ApiResultMeta> = async (
  args,
  api,
  extraOptions,
) => {
  const request: ApiRequest = typeof args === 'string' ? { url: args } : args;
  const meta: ApiResultMeta = { silent: extraOptions?.silent === true };

  const first = await send(request, api.signal);
  if (!('error' in first) || first.error.status !== 401 || isSessionRequest(request.url)) return { ...first, meta };

  const hadToken = getAccessToken() !== null;
  const token = await refreshAccessToken();
  if (token) {
    const second = await send(request, api.signal);
    if (!('error' in second) || second.error.status !== 401) return { ...second, meta };
    return { error: { ...second.error, sessionEnded: true }, meta };
  }
  // A visitor who never had a token is simply anonymous; a user whose refresh was refused was signed out
  return { error: { ...first.error, sessionEnded: hadToken }, meta };
};

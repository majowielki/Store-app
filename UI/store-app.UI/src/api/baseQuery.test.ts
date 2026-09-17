import { http, HttpResponse } from 'msw';
import { describe, expect, it, vi } from 'vitest';
import { baseQuery } from './baseQuery';
import { getAccessToken, onSessionEnded, setAccessToken } from './session';
import { session } from '@/test/fixtures';
import { api, json, problemResponse } from '@/test/handlers';
import { server } from '@/test/server';

const call = (args: Parameters<typeof baseQuery>[0], extra?: Parameters<typeof baseQuery>[2]) =>
  baseQuery(args, { signal: new AbortController().signal, dispatch: vi.fn(), getState: vi.fn(), extra: undefined, endpoint: 'test', type: 'query', abort: vi.fn() }, extra ?? {});

describe('baseQuery', () => {
  it('sends the bearer token and the query string, and returns the JSON body', async () => {
    let seen: Request | undefined;
    server.use(
      http.get(api('/products'), ({ request }) => {
        seen = request;
        return json({ items: [] });
      }),
    );
    setAccessToken('abc');

    const result = await call({ url: '/products', params: { search: 'oak', page: 2, empty: '', missing: undefined } });

    expect(result.data).toEqual({ items: [] });
    expect(seen?.headers.get('Authorization')).toBe('Bearer abc');
    expect(new URL(seen!.url).search).toBe('?search=oak&page=2');
  });

  it('turns a problem response into an ApiError with the detail as the message', async () => {
    server.use(http.get(api('/products/1'), () => problemResponse(404, 'Product 1 was not found')));

    const result = await call('/products/1');

    expect(result.error).toMatchObject({ status: 404, message: 'Product 1 was not found' });
    expect(result.error?.problem?.traceId).toBe('00-test-00');
    expect(result.meta).toEqual({ silent: false });
  });

  it('appends the field messages of a validation problem', async () => {
    server.use(http.post(api('/products'), () => problemResponse(422, 'Please correct the form', { Price: ['must be positive'], Title: ['is required'] })));

    const result = await call({ url: '/products', method: 'POST', body: {} });

    expect(result.error?.message).toBe('Please correct the form: must be positive is required');
  });

  it('refreshes the access token after a 401 and repeats the request once', async () => {
    const tokens: (string | null)[] = [];
    server.use(
      http.get(api('/cart'), ({ request }) => {
        const token = request.headers.get('Authorization');
        tokens.push(token);
        return token === 'Bearer fresh' ? json({ items: [] }) : problemResponse(401, 'expired');
      }),
      http.post(api('/auth/refresh'), () => json({ ...session(), accessToken: 'fresh' })),
    );
    setAccessToken('stale');

    const result = await call('/cart');

    expect(result.data).toEqual({ items: [] });
    expect(tokens).toEqual(['Bearer stale', 'Bearer fresh']);
    expect(getAccessToken()).toBe('fresh');
  });

  it('ends the session when the refresh is refused and marks the error', async () => {
    server.use(http.get(api('/cart'), () => problemResponse(401, 'expired')));
    setAccessToken('stale');
    const ended = vi.fn();
    const unsubscribe = onSessionEnded(ended);

    const result = await call('/cart');

    unsubscribe();
    expect(result.error).toMatchObject({ status: 401, sessionEnded: true });
    expect(ended).toHaveBeenCalledTimes(1);
    expect(getAccessToken()).toBeNull();
  });

  it('does not treat a 401 from the login endpoint as an expired token', async () => {
    let refreshed = false;
    server.use(
      http.post(api('/auth/login'), () => problemResponse(401, 'Invalid email or password')),
      http.post(api('/auth/refresh'), () => {
        refreshed = true;
        return problemResponse(401, 'no');
      }),
    );

    const result = await call({ url: '/auth/login', method: 'POST', body: { email: 'a', password: 'b' } });

    expect(result.error).toMatchObject({ status: 401, message: 'Invalid email or password' });
    expect(result.error?.sessionEnded).toBeUndefined();
    expect(refreshed).toBe(false);
  });

  it('reports a network failure without a problem', async () => {
    server.use(http.get(api('/products'), () => HttpResponse.error()));

    const result = await call('/products');

    expect(result.error).toMatchObject({ status: 'FETCH_ERROR' });
    expect(result.error?.message).toMatch(/could not be reached/);
  });

  it('passes the silent flag of the endpoint on in the result meta', async () => {
    server.use(http.get(api('/auth/me'), () => problemResponse(401, 'no')));

    const result = await call('/auth/me', { silent: true });

    expect(result.meta).toEqual({ silent: true });
  });
});

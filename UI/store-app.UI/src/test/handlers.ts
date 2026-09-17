import { HttpResponse, http, type DefaultBodyType, type HttpResponseInit } from 'msw';
import { apiBaseUrl } from '@/config';
import type { ProblemDetails } from '@/api/types';
import { cartWithTable, emptyCart, meta, order, pricingRules, problem, product, session, user } from './fixtures';

// Node's fetch needs absolute URLs, so vitest.config.ts sets VITE_API_BASE_URL and the app and the
// handlers read the same value
export const api = (path: string) => `${apiBaseUrl}${path}`;

/** A problem response, the way the API answers every failure. */
export const problemResponse = (status: number, detail: string, errors?: Record<string, string[]>) =>
  HttpResponse.json<ProblemDetails>(problem(status, detail, errors), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  });

export const json = <T extends DefaultBodyType>(body: T, init?: HttpResponseInit) => HttpResponse.json<T>(body, init);

export const page = <T>(items: T[]) => ({
  items,
  totalCount: items.length,
  page: 1,
  pageSize: 20,
  totalPages: 1,
  hasNextPage: false,
  hasPreviousPage: false,
});

/** The happy path: a visitor, a catalogue of one product and a signed-in user with a cart. */
export const handlers = [
  http.post(api('/auth/refresh'), () => problemResponse(401, 'No session')),
  http.get(api('/auth/me'), ({ request }) =>
    request.headers.get('Authorization') ? json(user) : new HttpResponse(null, { status: 204 }),
  ),
  http.post(api('/auth/login'), () => json(session())),
  http.post(api('/auth/demo-login'), () => json(session())),
  http.post(api('/auth/logout'), () => new HttpResponse(null, { status: 204 })),

  http.get(api('/products'), () => json(page([product()]))),
  http.get(api('/products/meta'), () => json(meta)),
  http.get(api('/products/:id'), ({ params }) => (params.id === '7' ? json(product()) : problemResponse(404, 'Product 999 was not found'))),

  http.get(api('/cart'), () => json(cartWithTable())),
  http.post(api('/cart/items'), () => json(cartWithTable(2))),
  http.put(api('/cart/items/:id'), () => json(cartWithTable(3))),
  http.delete(api('/cart/items/:id'), () => json(emptyCart)),
  http.post(api('/cart/sync'), () => json(cartWithTable())),

  http.get(api('/orders/pricing-rules'), () => json(pricingRules)),
  http.get(api('/orders/has-orders'), () => json({ hasOrders: false, ordersCount: 0 })),
  http.get(api('/orders/my-orders'), () => json(page([order()]))),
  http.post(api('/orders/from-cart'), () => json(order(), { status: 201 })),
];

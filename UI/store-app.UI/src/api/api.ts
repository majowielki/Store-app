import { createApi } from '@reduxjs/toolkit/query/react';
import { baseQuery } from './baseQuery';

/** What a mutation can make stale: every query of a resource carries one of these. */
const tagTypes = ['Cart', 'Wishlist', 'Orders', 'Products', 'Users', 'Stats', 'Content', 'DiscountCodes', 'Reviews'] as const;
export type ApiTag = (typeof tagTypes)[number];

/**
 * The store's API as RTK Query sees it: one cache, one base query (src/api/baseQuery.ts),
 * endpoints injected per document from the files next to this one (auth, cart, catalog,
 * content, orders, admin). Hooks are exported by those files.
 */
export const api = createApi({
  reducerPath: 'api',
  baseQuery,
  tagTypes,
  endpoints: () => ({}),
});

/** Tags a mutation makes stale when it succeeds; a refused change (403, 422) leaves the cache as it is. */
export const unlessFailed =
  (tags: ApiTag[]) =>
  (_result: unknown, error: unknown): ApiTag[] =>
    error ? [] : tags;

/** Seconds an unused answer stays cached when it is the shop's configuration, the same for everyone: the whole visit. */
export const KEEP_FOR_THE_VISIT = 24 * 60 * 60;

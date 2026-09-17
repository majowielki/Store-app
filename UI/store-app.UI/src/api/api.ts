import { createApi } from '@reduxjs/toolkit/query/react';
import { baseQuery } from './baseQuery';

/** What a mutation can make stale: every query of a resource carries one of these. */
const tagTypes = ['Cart', 'Orders', 'Products', 'Users', 'Stats'] as const;
export type ApiTag = (typeof tagTypes)[number];

/**
 * The store's API as RTK Query sees it: one cache, one base query (src/api/baseQuery.ts),
 * endpoints injected per document from the files next to this one (auth, cart, catalog,
 * orders, admin). Hooks are exported by those files.
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

import { api } from './api';
import type { Funnel, ShopEventKind } from './types';

export const funnelApi = api.injectEndpoints({
  endpoints: (build) => ({
    /**
     * Counts a step towards buying for the purchase funnel: the product and what happened, nothing
     * about the visitor. Silent: a count that gets lost is no business of the visitor's.
     */
    recordShopEvent: build.mutation<void, { kind: ShopEventKind; productId: number }>({
      query: (body) => ({ url: '/shop-events', method: 'POST', body }),
      extraOptions: { silent: true },
    }),
    /** The funnel over the dashboard's window; a new or paid order makes it stale with the other statistics. */
    getFunnel: build.query<Funnel, { days: number }>({
      query: (params) => ({ url: '/auditlog/funnel', params }),
      providesTags: ['Stats'],
    }),
  }),
});

export const { useRecordShopEventMutation, useGetFunnelQuery } = funnelApi;

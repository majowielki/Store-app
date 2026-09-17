import type { TypedMutationOnQueryStarted } from '@reduxjs/toolkit/query';
import { api } from './api';
import type { baseQuery } from './baseQuery';
import type { AddCartItemRequest, ApiCartResponse, SyncCartRequest, UpdateCartItemRequest } from './types';

/** A cart with nothing in it, as the API describes one that was never created. */
export const emptyCart: ApiCartResponse = {
  id: 0,
  userId: '',
  items: [],
  totalItems: 0,
  total: 0,
  updatedAt: new Date(0).toISOString(),
  isEmpty: true,
  priceChanged: false,
};

/** Every change answers with the whole cart afterwards; that answer becomes the cached cart. */
const replaceCart: TypedMutationOnQueryStarted<ApiCartResponse, unknown, typeof baseQuery, 'api'> = async (
  _,
  { dispatch, queryFulfilled },
) => {
  try {
    const { data } = await queryFulfilled;
    dispatch(cartApi.util.upsertQueryData('getCart', undefined, data));
  } catch {
    // A refused change leaves the cached cart as it was; the error middleware reports it
  }
};

/**
 * The signed-in customer's cart on the server. Every change answers with the whole cart as
 * it is afterwards, so a mutation writes that answer straight into the cache of getCart
 * instead of asking for it again.
 */
export const cartApi = api.injectEndpoints({
  endpoints: (build) => ({
    getCart: build.query<ApiCartResponse, void>({
      query: () => '/cart',
      providesTags: ['Cart'],
    }),
    addCartItem: build.mutation<ApiCartResponse, AddCartItemRequest>({
      query: (body) => ({ url: '/cart/items', method: 'POST', body }),
      onQueryStarted: replaceCart,
    }),
    updateCartItem: build.mutation<ApiCartResponse, { itemId: number } & UpdateCartItemRequest>({
      query: ({ itemId, ...body }) => ({ url: `/cart/items/${itemId}`, method: 'PUT', body }),
      onQueryStarted: replaceCart,
    }),
    removeCartItem: build.mutation<ApiCartResponse, number>({
      query: (itemId) => ({ url: `/cart/items/${itemId}`, method: 'DELETE' }),
      onQueryStarted: replaceCart,
    }),
    /** Merges the lines of a guest cart into the server cart, once, at sign-in. */
    syncCart: build.mutation<ApiCartResponse, SyncCartRequest>({
      query: (body) => ({ url: '/cart/sync', method: 'POST', body }),
      onQueryStarted: replaceCart,
    }),
  }),
});

export const {
  useGetCartQuery,
  useAddCartItemMutation,
  useUpdateCartItemMutation,
  useRemoveCartItemMutation,
} = cartApi;

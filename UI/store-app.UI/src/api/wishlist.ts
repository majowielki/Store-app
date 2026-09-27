import type { TypedMutationOnQueryStarted } from '@reduxjs/toolkit/query';
import { api } from './api';
import type { baseQuery } from './baseQuery';
import type { Wishlist } from './types';

/** Every change answers with the whole list afterwards; that answer becomes the cached list. */
const replaceWishlist: TypedMutationOnQueryStarted<Wishlist, unknown, typeof baseQuery, 'api'> = async (_, { dispatch, queryFulfilled }) => {
  try {
    const { data } = await queryFulfilled;
    dispatch(wishlistApi.util.upsertQueryData('getWishlist', undefined, data));
  } catch {
    // A refused change leaves the cached list as it was; the error middleware reports it
  }
};

/** The signed-in customer's wishlist on the server: product ids, the one added last first. */
export const wishlistApi = api.injectEndpoints({
  endpoints: (build) => ({
    getWishlist: build.query<Wishlist, void>({
      query: () => '/wishlist',
      providesTags: ['Wishlist'],
    }),
    addToWishlist: build.mutation<Wishlist, number>({
      query: (productId) => ({ url: '/wishlist/items', method: 'POST', body: { productId } }),
      onQueryStarted: replaceWishlist,
    }),
    removeFromWishlist: build.mutation<Wishlist, number>({
      query: (productId) => ({ url: `/wishlist/items/${productId}`, method: 'DELETE' }),
      onQueryStarted: replaceWishlist,
    }),
    syncWishlist: build.mutation<Wishlist, number[]>({
      query: (productIds) => ({ url: '/wishlist/sync', method: 'POST', body: { productIds } }),
      onQueryStarted: replaceWishlist,
    }),
  }),
});

export const { useGetWishlistQuery, useAddToWishlistMutation, useRemoveFromWishlistMutation } = wishlistApi;

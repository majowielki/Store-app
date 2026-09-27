import { useCallback, useMemo } from 'react';
import { useAddToWishlistMutation, useGetWishlistQuery, useRemoveFromWishlistMutation } from '@/api/wishlist';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { unwished, wished } from './guestWishlistSlice';

export interface WishlistView {
  /** Product ids on the list, the one added last first. */
  productIds: number[];
  has: (productId: number) => boolean;
  /** Puts the product on the list or takes it off; a refused change throws (it has been reported). */
  toggle: (productId: number) => Promise<void>;
}

/** The wishlist: the server's for a signed-in customer, this browser's for a visitor. */
export const useWishlist = (): WishlistView => {
  const user = useAppSelector((state) => state.session.user);
  const guestIds = useAppSelector((state) => state.guestWishlist.productIds);
  const dispatch = useAppDispatch();
  const { data } = useGetWishlistQuery(undefined, { skip: !user });
  const [add] = useAddToWishlistMutation();
  const [remove] = useRemoveFromWishlistMutation();

  const productIds = useMemo(() => (user ? (data?.items ?? []).map((item) => item.productId) : guestIds), [user, data, guestIds]);
  const has = useCallback((productId: number) => productIds.includes(productId), [productIds]);

  const toggle = useCallback(
    async (productId: number) => {
      const on = productIds.includes(productId);
      if (!user) {
        dispatch(on ? unwished(productId) : wished(productId));
      } else if (on) {
        await remove(productId).unwrap();
      } else {
        await add(productId).unwrap();
      }
    },
    [user, productIds, dispatch, add, remove],
  );

  return { productIds, has, toggle };
};

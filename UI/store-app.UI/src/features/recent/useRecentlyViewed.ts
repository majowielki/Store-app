import { useEffect, useMemo } from 'react';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { useProductsById } from '@/hooks/use-products-by-id';
import { productViewed } from './recentlyViewedSlice';

/** Records that a product page was opened. */
export const useTrackProductView = (productId: number) => {
  const dispatch = useAppDispatch();
  useEffect(() => {
    dispatch(productViewed(productId));
  }, [dispatch, productId]);
};

/** The products viewed lately, the latest first, without the one on the page now. */
export const useRecentlyViewed = (excludeId?: number) => {
  const all = useAppSelector((state) => state.recentlyViewed.productIds);
  const ids = useMemo(() => all.filter((id) => id !== excludeId), [all, excludeId]);
  return useProductsById(ids);
};

import { useCallback } from 'react';
import type { Product } from '@/api/types';
import { useAppDispatch } from '@/hooks';
import { priceTag } from '@/utils/productPrice';
import { drawerOpened, setDrawerReturnFocus } from './cartDrawerSlice';
import { useCartActions } from './useCart';

/**
 * Puts a product in the bag at the price the page shows (the sale price on a sale) and slides
 * the bag in. A refused line throws; its error has been shown already.
 */
export const useAddToBag = () => {
  const { add } = useCartActions();
  const dispatch = useAppDispatch();

  return useCallback(
    async (product: Product, color: string, quantity: number, returnFocusTo?: HTMLElement | null) => {
      await add({
        productId: product.id,
        title: product.title,
        image: product.image,
        company: product.company,
        color,
        unitPrice: priceTag(product).effectivePrice,
        quantity,
      });
      setDrawerReturnFocus(returnFocusTo ?? null);
      dispatch(drawerOpened({ productId: product.id, slug: product.slug, category: product.category }));
    },
    [add, dispatch],
  );
};

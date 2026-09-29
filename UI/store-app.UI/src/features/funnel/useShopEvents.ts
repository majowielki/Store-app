import { useCallback, useEffect, useRef } from 'react';
import { useRecordShopEventMutation } from '@/api/funnel';

/** Counts a product's page or quick view as seen, once per product it shows. */
export const useCountProductView = (productId: number | undefined) => {
  const [record] = useRecordShopEventMutation();
  // A ref outlives React's double run of effects in development, so a view is counted once
  const counted = useRef<number | undefined>(undefined);

  useEffect(() => {
    if (productId === undefined || counted.current === productId) return;
    counted.current = productId;
    void record({ kind: 'productViewed', productId });
  }, [productId, record]);
};

/** Counts a product put in the bag. */
export const useCountAddedToBag = () => {
  const [record] = useRecordShopEventMutation();
  return useCallback((productId: number) => void record({ kind: 'addedToBag', productId }), [record]);
};

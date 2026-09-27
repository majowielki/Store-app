import { useMemo } from 'react';
import { useGetProductsQuery } from '@/api/catalog';
import type { Product } from '@/api/types';

/**
 * The catalogue products for a list of ids kept in the browser (wishlist, recently viewed,
 * compared), in the list's order. One request fetches them all; an id the catalogue no longer
 * lists (a deactivated product) is simply left out.
 */
export const useProductsById = (ids: readonly number[]) => {
  const key = ids.join(',');
  const { data, isLoading } = useGetProductsQuery({ ids: key, pageSize: 100 }, { skip: key.length === 0 });

  const products = useMemo(() => {
    const byId = new Map((data?.items ?? []).map((product) => [product.id, product]));
    return ids.map((id) => byId.get(id)).filter((product): product is Product => product !== undefined);
  }, [data, ids]);

  return { products, isLoading: isLoading && key.length > 0 };
};

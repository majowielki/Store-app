import { useMemo } from 'react';
import { useGetProductsQuery } from '@/api/catalog';
import type { Product } from '@/api/types';

/**
 * The catalogue products a piece of content names, in the order it names them. Content refers
 * to products by slug; one request fetches them all, and a slug the catalogue no longer lists
 * (a deactivated product) is simply left out.
 */
export const useProductsBySlug = (slugs: readonly string[] | undefined) => {
  const key = slugs?.join(',') ?? '';
  const { data, isLoading } = useGetProductsQuery({ slugs: key, pageSize: 100 }, { skip: key.length === 0 });

  const products = useMemo(() => {
    const bySlug = new Map((data?.items ?? []).map((product) => [product.slug, product]));
    return (slugs ?? []).map((slug) => bySlug.get(slug)).filter((product): product is Product => product !== undefined);
  }, [data, slugs]);

  return { products, bySlug: useMemo(() => new Map(products.map((p) => [p.slug, p])), [products]), isLoading: isLoading && key.length > 0 };
};

import { catalogApi } from '@/api/catalog';
import type { Product } from '@/api/types';
import { useAppDispatch } from './redux';

/**
 * What a product link does on click, before the router navigates: the product the listing
 * already holds goes into the product page's cache, so the page renders at once instead of
 * waiting for it, and the tile's image is named for the view transition to the page's image.
 * The listing has no gallery; the page asks for the whole product again and adds it.
 */
export const useOpenProduct = () => {
  const dispatch = useAppDispatch();
  return (product: Product, image?: HTMLElement | null) => {
    void dispatch(catalogApi.util.upsertQueryData('getProduct', product.id, { ...product, images: [], hotspots: [] }));
    // One element per name: another tile may still carry it from an earlier click
    document.querySelectorAll('[data-vt="product-image"]').forEach((element) => element.removeAttribute('data-vt'));
    image?.setAttribute('data-vt', 'product-image');
  };
};

import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { ArrowRight, X } from 'lucide-react';
import { useGetProductsQuery } from '@/api/catalog';
import { useGetCollectionsQuery } from '@/api/content';
import { drawerClosed, takeDrawerReturnFocus, type AddedProduct } from '@/features/cart/cartDrawerSlice';
import { useCart, useCartActions } from '@/features/cart/useCart';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { useProductsBySlug } from '@/hooks/use-products-by-slug';
import { formatAsDollars, type Product } from '@/utils';
import DeliveryEstimate from './DeliveryEstimate';
import FreeDeliveryProgress from './FreeDeliveryProgress';
import ProductPrice from './ProductPrice';
import { Button } from './ui/button';
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from './ui/sheet';

const SUGGESTIONS = 3;

/**
 * What goes with the piece just added: the other pieces of a collection it belongs to, or
 * else more of its category - never something already in the bag.
 */
const useCompleteTheLook = (added: AddedProduct | null, inBag: Set<number>) => {
  const { data: collections, isLoading } = useGetCollectionsQuery(undefined, { skip: !added });
  const collection = added ? collections?.find((c) => c.productSlugs.includes(added.slug)) : undefined;
  const { products: fromCollection, isLoading: collectionLoading } = useProductsBySlug(collection?.productSlugs);
  const wanted = (p: Product) => p.id !== added?.productId && !inBag.has(p.id);
  const collectionHasMore = fromCollection.some(wanted);
  const { data: sameCategory } = useGetProductsQuery(
    { category: added?.category, pageSize: 12 },
    // Without the content service, or with the whole collection in the bag, the category suggests
    { skip: !added || isLoading || collectionLoading || collectionHasMore },
  );

  return useMemo(() => {
    const fromCollectionLeft = fromCollection.filter((p) => p.id !== added?.productId && !inBag.has(p.id));
    const showCollection = collection !== undefined && fromCollectionLeft.length > 0;
    const pool: Product[] = showCollection ? fromCollectionLeft : (sameCategory?.items ?? []).filter((p) => p.id !== added?.productId && !inBag.has(p.id));
    return { products: pool.slice(0, SUGGESTIONS), source: showCollection ? `From the ${collection.title} collection` : 'More like this' };
  }, [collection, fromCollection, sameCategory, added, inBag]);
};

/**
 * The bag sliding in after "Add to bag": the lines, the subtotal, the way to free delivery and
 * three pieces to complete the look. Escape, a click outside or the close button close it and
 * the focus goes back to the button that opened it.
 */
const CartDrawer = () => {
  const dispatch = useAppDispatch();
  const { open, lastAdded } = useAppSelector((state) => state.cartDrawer);
  const user = useAppSelector((state) => state.session.user);
  const { lines, subtotal, totalItems } = useCart();
  const { remove } = useCartActions();
  const inBag = useMemo(() => new Set(lines.map((line) => line.productId)), [lines]);
  const { products: suggestions, source } = useCompleteTheLook(open ? lastAdded : null, inBag);
  const close = () => dispatch(drawerClosed());

  return (
    <Sheet open={open} onOpenChange={(isOpen) => !isOpen && close()}>
      <SheetContent
        className="flex w-full flex-col gap-0 overflow-y-auto p-0 sm:max-w-md"
        onCloseAutoFocus={(event) => {
          const target = takeDrawerReturnFocus();
          if (target) {
            event.preventDefault();
            target.focus();
          }
        }}
      >
        <SheetHeader className="border-b p-6 pr-16 text-left">
          <SheetTitle className="display text-3xl font-normal">Added to your bag</SheetTitle>
          <SheetDescription>
            {totalItems} {totalItems === 1 ? 'piece' : 'pieces'} · {formatAsDollars(subtotal)}
          </SheetDescription>
        </SheetHeader>

        <div className="flex-1 p-6">
          <ul aria-label="In your bag" className="grid gap-4">
            {lines.map((line) => (
              <li key={line.key} className="flex items-center gap-4">
                <Link to={`/products/${line.productId}`} onClick={close} className="block h-16 w-20 shrink-0 overflow-hidden rounded-xl bg-muted">
                  {line.image && <img src={line.image} alt="" className="h-full w-full object-cover" />}
                </Link>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">{line.title}</p>
                  <p className="text-xs capitalize text-muted-foreground">
                    {line.color} · {line.quantity} × {formatAsDollars(line.unitPrice)}
                  </p>
                </div>
                <span className="text-sm tabular-nums">{formatAsDollars(line.lineTotal)}</span>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="h-8 w-8 shrink-0 text-muted-foreground"
                  aria-label={`Remove ${line.title}`}
                  onClick={() => remove(line).catch(() => undefined)}
                >
                  <X className="h-4 w-4" />
                </Button>
              </li>
            ))}
          </ul>

          <FreeDeliveryProgress subtotal={subtotal} />
          <DeliveryEstimate className="mt-4" />

          {suggestions.length > 0 && (
            <section aria-labelledby="complete-the-look" className="mt-8">
              <h3 id="complete-the-look" className="display text-2xl">
                Complete the look
              </h3>
              <p className="text-xs text-muted-foreground">{source}</p>
              <ul className="mt-4 grid grid-cols-3 gap-3">
                {suggestions.map((product) => (
                  <li key={product.id}>
                    <Link to={`/products/${product.id}`} onClick={close} className="group block">
                      <div className="aspect-square overflow-hidden rounded-xl bg-muted">
                        <img src={product.image} alt="" className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105" />
                      </div>
                      <p className="mt-2 line-clamp-2 text-xs font-medium leading-snug">{product.title}</p>
                      <p className="text-xs">
                        <ProductPrice product={product} />
                      </p>
                    </Link>
                  </li>
                ))}
              </ul>
            </section>
          )}
        </div>

        <div className="sticky bottom-0 grid gap-2 border-t bg-background p-6">
          <Button asChild size="lg" className="group w-full">
            <Link to={user ? '/checkout' : '/login'} onClick={close}>
              {user ? 'Checkout' : 'Sign in to check out'}
              <ArrowRight className="transition-transform duration-300 group-hover:translate-x-1" />
            </Link>
          </Button>
          <Button asChild variant="outline" size="lg" className="w-full">
            <Link to="/cart" onClick={close}>
              View bag
            </Link>
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
};

export default CartDrawer;

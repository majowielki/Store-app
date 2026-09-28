import { useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowRight, Heart, ShoppingBag, X } from 'lucide-react';
import Loading from '@/components/Loading';
import ProductPrice from '@/components/ProductPrice';
import { Button } from '@/components/ui/button';
import { useAddToBag } from '@/features/cart/useAddToBag';
import { useWishlist } from '@/features/wishlist/useWishlist';
import { useAppSelector } from '@/hooks';
import { useProductsById } from '@/hooks/use-products-by-id';
import { usePageMeta } from '@/seo';
import type { Product } from '@/utils';

/** One piece on the list: into the bag in its first colour (and off the list), or just off the list. */
const WishlistRow = ({ product }: { product: Product }) => {
  const { toggle } = useWishlist();
  const addToBag = useAddToBag();
  const [busy, setBusy] = useState(false);

  const moveToBag = async (event: React.MouseEvent<HTMLButtonElement>) => {
    const button = event.currentTarget;
    setBusy(true);
    try {
      await addToBag(product, product.colors[0] ?? '', 1, button);
      await toggle(product.id);
    } catch {
      // reported by the error middleware
    } finally {
      setBusy(false);
    }
  };

  return (
    <li className="grid grid-cols-[6rem_1fr] gap-5 py-6 sm:grid-cols-[8.5rem_1fr_auto] sm:items-center">
      <Link to={`/products/${product.id}`} className="block aspect-5/4 overflow-hidden rounded-xl bg-muted">
        <img src={product.image} alt={product.title} className="h-full w-full object-cover" />
      </Link>
      <div className="min-w-0">
        <p className="eyebrow">{product.company}</p>
        <h2 className="mt-1 font-medium">
          <Link to={`/products/${product.id}`} className="hover:underline">
            {product.title}
          </Link>
        </h2>
        <p className="mt-1 text-sm">
          <ProductPrice product={product} />
        </p>
      </div>
      <div className="col-start-2 flex gap-2 sm:col-start-auto">
        <Button type="button" onClick={moveToBag} disabled={busy}>
          <ShoppingBag />
          Move to bag
        </Button>
        <Button type="button" variant="ghost" size="icon" aria-label={`Remove ${product.title} from your wishlist`} onClick={() => toggle(product.id).catch(() => undefined)}>
          <X />
        </Button>
      </div>
    </li>
  );
};

/** The wishlist: the pieces kept for later, with today's prices, the one added last first. */
const Wishlist = () => {
  usePageMeta({ title: 'Wishlist', noindex: true });
  const user = useAppSelector((state) => state.session.user);
  const { productIds } = useWishlist();
  // In the list's order, without what the catalogue no longer sells
  const { products, isLoading } = useProductsById(productIds);

  if (isLoading) return <Loading />;

  if (products.length === 0) {
    return (
      <div className="grid place-items-center py-16 text-center md:py-24">
        <span className="grid h-20 w-20 place-items-center rounded-full bg-secondary">
          <Heart className="h-8 w-8" />
        </span>
        <h1 className="display mt-8 text-5xl md:text-6xl">Your wishlist is empty</h1>
        <p className="mt-4 max-w-sm text-muted-foreground">Tap the heart on any piece to keep it here for later.</p>
        <Button asChild size="lg" className="group mt-10">
          <Link to="/products">
            Browse the shop
            <ArrowRight className="transition-transform duration-300 group-hover:translate-x-1" />
          </Link>
        </Button>
      </div>
    );
  }

  return (
    <>
      <header>
        <p className="eyebrow">Kept for later</p>
        <div className="mt-3 flex flex-wrap items-end justify-between gap-4">
          <h1 className="display text-5xl md:text-7xl">Wishlist</h1>
          <p className="text-sm text-muted-foreground">
            {products.length} {products.length === 1 ? 'piece' : 'pieces'}
          </p>
        </div>
        {!user && (
          <p className="mt-4 text-sm text-muted-foreground">
            Saved in this browser. <Link to="/login" className="link-underline font-medium text-foreground">Sign in</Link> to keep it on your account.
          </p>
        )}
      </header>
      <ul aria-label="Wishlist" className="mt-8 divide-y border-y">
        {products.map((product) => (
          <WishlistRow key={product.id} product={product} />
        ))}
      </ul>
    </>
  );
};

export default Wishlist;

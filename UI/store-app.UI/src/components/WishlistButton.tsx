import { Heart } from 'lucide-react';
import { useWishlist } from '@/features/wishlist/useWishlist';
import { toast } from '@/hooks/use-toast';
import { cn } from '@/lib/utils';
import type { Product } from '@/utils';

/**
 * The heart: puts a product on the wishlist or takes it off. A visitor's list stays in the
 * browser until they sign in. The button says what it will do and whether it is pressed.
 */
const WishlistButton = ({ product, className }: { product: Pick<Product, 'id' | 'title'>; className?: string }) => {
  const { has, toggle } = useWishlist();
  const saved = has(product.id);

  const onClick = async () => {
    try {
      await toggle(product.id);
      toast({ description: saved ? `${product.title} removed from your wishlist.` : `${product.title} saved to your wishlist.` });
    } catch {
      // reported by the error middleware
    }
  };

  return (
    <button
      type="button"
      aria-pressed={saved}
      aria-label={saved ? `Remove ${product.title} from your wishlist` : `Save ${product.title} to your wishlist`}
      onClick={onClick}
      className={cn(
        'grid h-10 w-10 place-items-center rounded-full bg-background/90 shadow-md backdrop-blur-sm transition-transform duration-300 hover:scale-110 focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring',
        className,
      )}
    >
      {/* Saving fills the heart from the bottom up, like water in a glass; the outline stays on top */}
      <span className="relative h-[1.1rem] w-[1.1rem]">
        <Heart
          aria-hidden
          className={cn(
            'absolute inset-0 h-full w-full fill-brand text-brand transition-[clip-path] duration-500 ease-smooth',
            saved ? '[clip-path:inset(0_0_0_0)]' : '[clip-path:inset(100%_0_0_0)]',
          )}
        />
        <Heart aria-hidden className={cn('absolute inset-0 h-full w-full transition-colors', saved ? 'text-brand' : 'text-foreground')} />
      </span>
    </button>
  );
};

export default WishlistButton;

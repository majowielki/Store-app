import { useEffect, useId, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowUpRight, ShoppingBag } from 'lucide-react';
import type { Hotspot, Product } from '@/api/types';
import { ProductPrice } from '@/components/ProductCard';
import { Button } from '@/components/ui/button';
import { useCartActions } from '@/features/cart/useCart';
import { toast } from '@/hooks/use-toast';
import { cn } from '@/lib/utils';
import { formatAsDollars, priceTag } from '@/utils';

interface LookbookPictureProps {
  image: string;
  alt: string;
  hotspots: Hotspot[];
  /** The products the points stand for, by slug; a point without one is not drawn. */
  products: Map<string, Product>;
  className?: string;
  /** The picture is the page's first image: load it right away. */
  priority?: boolean;
}

/**
 * A room picture with a button on every product in it. A button names its product and price for
 * screen readers; pressing it opens a small card with a link to the product. Escape, a click
 * elsewhere or the same button closes the card again.
 */
const LookbookPicture = ({ image, alt, hotspots, products, className, priority = false }: LookbookPictureProps) => {
  const [open, setOpen] = useState<string | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const baseId = useId();

  useEffect(() => {
    if (!open) return;
    const onKey = (event: KeyboardEvent) => event.key === 'Escape' && setOpen(null);
    const onPointer = (event: PointerEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(null);
    };
    document.addEventListener('keydown', onKey);
    document.addEventListener('pointerdown', onPointer);
    return () => {
      document.removeEventListener('keydown', onKey);
      document.removeEventListener('pointerdown', onPointer);
    };
  }, [open]);

  const points = hotspots.filter((point) => products.has(point.productSlug));

  return (
    <div ref={containerRef} className={cn('relative overflow-hidden rounded-[2rem] bg-muted', className)}>
      <img src={image} alt={alt} loading={priority ? 'eager' : 'lazy'} className="block h-auto w-full" />
      {points.map((point, index) => {
        const product = products.get(point.productSlug)!;
        const isOpen = open === point.productSlug;
        const cardId = `${baseId}-${index}`;
        const { effectivePrice } = priceTag(product);
        return (
          <div key={point.productSlug} className="absolute" style={{ left: `${point.x}%`, top: `${point.y}%` }}>
            <button
              type="button"
              aria-label={`${product.title}, ${formatAsDollars(effectivePrice)}`}
              aria-expanded={isOpen}
              aria-controls={cardId}
              onClick={() => setOpen(isOpen ? null : point.productSlug)}
              className="group relative grid h-8 w-8 -translate-x-1/2 -translate-y-1/2 place-items-center rounded-full focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
            >
              <span className="absolute inset-0 animate-ping rounded-full bg-white/60 motion-reduce:hidden [animation-duration:2.4s]" />
              <span
                className={cn(
                  'relative grid h-7 w-7 place-items-center rounded-full border border-white/70 bg-white/90 shadow-lg backdrop-blur transition-transform duration-300 group-hover:scale-110',
                  isOpen && 'scale-110 bg-brand',
                )}
              >
                <span className={cn('h-2 w-2 rounded-full bg-foreground', isOpen && 'bg-white')} />
              </span>
            </button>
            {isOpen && (
              <div
                id={cardId}
                role="dialog"
                aria-label={product.title}
                className={cn(
                  'absolute z-10 w-64 rounded-2xl border bg-background/95 p-3 text-left shadow-xl backdrop-blur animate-in fade-in-0 zoom-in-95',
                  point.x > 60 ? 'right-6' : 'left-6',
                  point.y > 60 ? 'bottom-6' : 'top-6',
                )}
              >
                <Link to={`/products/${product.id}`} className="group/card flex gap-3">
                  <img src={product.image} alt="" className="h-16 w-16 shrink-0 rounded-xl object-cover" />
                  <span className="min-w-0">
                    <span className="eyebrow block text-[0.65rem]">{product.company}</span>
                    <span className="block text-sm font-medium leading-snug">{product.title}</span>
                    <span className="mt-1 flex items-center justify-between gap-2 text-sm">
                      <ProductPrice product={product} />
                      <ArrowUpRight className="h-4 w-4 shrink-0 transition-transform group-hover/card:-translate-y-0.5 group-hover/card:translate-x-0.5" />
                    </span>
                  </span>
                </Link>
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
};

/**
 * Puts every product of a look into the bag, one of each in its first colour. The lines are
 * added one after another; a refused line stops the rest (its error has been shown already).
 */
export const AddLookButton = ({ products, className }: { products: Product[]; className?: string }) => {
  const { add } = useCartActions();
  const [adding, setAdding] = useState(false);
  const total = products.reduce((sum, product) => sum + priceTag(product).effectivePrice, 0);

  const addAll = async () => {
    setAdding(true);
    try {
      for (const product of products) {
        await add({
          productId: product.id,
          title: product.title,
          image: product.image,
          company: product.company,
          color: product.colors[0] ?? '',
          unitPrice: priceTag(product).effectivePrice,
          quantity: 1,
        });
      }
      toast({ description: `${products.length} pieces added to your bag.` });
    } catch {
      // reported by the error middleware
    } finally {
      setAdding(false);
    }
  };

  return (
    <Button type="button" size="lg" onClick={addAll} disabled={adding || products.length === 0} className={className}>
      <ShoppingBag />
      {adding ? 'Adding…' : `Add the whole look · ${formatAsDollars(total)}`}
    </Button>
  );
};

export default LookbookPicture;

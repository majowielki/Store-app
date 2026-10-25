import { useEffect, useState, type MouseEvent, type RefObject } from 'react';
import { Check, ShoppingBag } from 'lucide-react';
import type { ProductDetail } from '@/api/types';
import { useFinishes } from '@/hooks/use-finishes';
import { cn } from '@/lib/utils';
import ProductPrice from './ProductPrice';
import ResponsiveImage from './ResponsiveImage';
import { Button } from './ui/button';

interface StickyBuyBarProps {
  product: ProductDetail;
  /** The finish chosen on the page. */
  color: string;
  /** The page's own add-to-bag row: the bar shows once it has scrolled away above the screen. */
  anchor: RefObject<HTMLElement | null>;
  onAdd: (event: MouseEvent<HTMLButtonElement>) => void;
  adding: boolean;
  added: boolean;
}

/**
 * A slim bar pinned to the bottom of the screen with the piece, its finish, its price and the bag
 * button, once the page's own button has scrolled away - so the reviews and the specifications
 * can be read without scrolling back up to buy. On a phone it sits above the bottom dock. Hidden
 * (and out of the accessibility tree) while the page's own button is on screen.
 */
const StickyBuyBar = ({ product, color, anchor, onAdd, adding, added }: StickyBuyBarProps) => {
  const { nameOf } = useFinishes();
  const [shown, setShown] = useState(false);

  useEffect(() => {
    const element = anchor.current;
    if (!element || typeof IntersectionObserver === 'undefined') return;
    let past = false;
    let atFooter = false;
    const update = () => setShown(past && !atFooter);
    // Shown only past the row (above the screen), not before it comes up from below...
    const row = new IntersectionObserver(([entry]) => {
      past = !entry.isIntersecting && entry.boundingClientRect.top < 0;
      update();
    });
    // ...and out of the way again once the footer comes up, so it never covers the site map
    const footer = new IntersectionObserver(([entry]) => {
      atFooter = entry.isIntersecting;
      update();
    });
    row.observe(element);
    const pageFooter = document.querySelector('footer');
    if (pageFooter) footer.observe(pageFooter);
    return () => {
      row.disconnect();
      footer.disconnect();
    };
  }, [anchor]);

  return (
    <div
      role="region"
      aria-label="Buy this piece"
      data-shown={shown}
      className={cn(
        'fixed inset-x-4 bottom-24 z-40 mx-auto flex max-w-xl items-center gap-3 rounded-full border bg-background/95 p-2 pl-2.5 shadow-[0_16px_40px_-12px_rgba(0,0,0,0.35)] backdrop-blur-xl transition-[opacity,transform,visibility] duration-500 ease-smooth md:bottom-6',
        shown ? 'visible translate-y-0 opacity-100' : 'invisible translate-y-4 opacity-0',
      )}
    >
      <ResponsiveImage src={product.image} size="thumbnail" alt="" className="h-10 w-10 shrink-0 rounded-full object-cover" />
      <div className="min-w-0 flex-1 text-sm leading-tight">
        <p className="truncate font-medium">{product.title}</p>
        <p className="truncate text-xs text-muted-foreground">
          <ProductPrice product={product} /> <span aria-hidden>·</span> {nameOf(color)}
        </p>
      </div>
      <Button size="sm" className="h-10 shrink-0 rounded-full px-4" onClick={onAdd} disabled={adding}>
        {added ? <Check aria-hidden /> : <ShoppingBag aria-hidden />}
        Add to bag
      </Button>
    </div>
  );
};

export default StickyBuyBar;

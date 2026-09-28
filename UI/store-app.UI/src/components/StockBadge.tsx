import { cn } from '@/lib/utils';
import type { Product } from '@/utils';

const pill = 'inline-flex items-center rounded-full px-2.5 py-1 text-[11px] font-semibold leading-none tracking-wide backdrop-blur-sm';

/** "Only 2 left" or "Sold out" over a product's picture; nothing while it is simply in stock. */
export const StockBadge = ({ product, className }: { product: Pick<Product, 'availability' | 'availableQuantity'>; className?: string }) => {
  if (product.availability === 'outOfStock') {
    return <span className={cn(pill, 'bg-foreground/85 text-background', className)}>Sold out</span>;
  }
  if (product.availability === 'lowStock') {
    return <span className={cn(pill, 'bg-background/90 text-foreground', className)}>Only {product.availableQuantity} left</span>;
  }
  return null;
};

const dot: Record<Product['availability'], string> = {
  inStock: 'bg-success',
  lowStock: 'bg-brand',
  outOfStock: 'bg-muted-foreground',
};

/** A line next to the price of a product: in stock, only a few left, or sold out. */
export const StockNote = ({ product, className }: { product: Pick<Product, 'availability' | 'availableQuantity'>; className?: string }) => {
  const text = {
    inStock: 'In stock',
    lowStock: `Only ${product.availableQuantity} left`,
    outOfStock: 'Sold out',
  }[product.availability];

  return (
    <p className={cn('flex items-center gap-2 text-sm', className)}>
      <span aria-hidden className={cn('h-2 w-2 rounded-full', dot[product.availability])} />
      {text}
    </p>
  );
};

export default StockBadge;

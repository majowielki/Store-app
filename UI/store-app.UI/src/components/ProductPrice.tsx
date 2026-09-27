import { cn } from '@/lib/utils';
import { formatAsDollars, priceTag, type Product } from '@/utils';

/** The price as every listing shows it: the sale price first, the list price struck through. */
const ProductPrice = ({ product, stacked = false }: { product: Product; stacked?: boolean }) => {
  const { price, effectivePrice, hasSale } = priceTag(product);
  return hasSale ? (
    <span className={cn('inline-flex gap-x-2', stacked ? 'flex-col items-end' : 'items-baseline')}>
      <span className="font-medium text-brand">{formatAsDollars(effectivePrice)}</span>
      <span className="text-sm text-muted-foreground line-through">{formatAsDollars(price)}</span>
    </span>
  ) : (
    <span className="font-medium">{formatAsDollars(price)}</span>
  );
};

export default ProductPrice;

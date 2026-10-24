import { cn } from '@/lib/utils';
import { formatAsDollars, priceTag, type Product } from '@/utils';

/** How the sale price and the struck-through list price stand: one over the other always, only from sm up, or never. */
type Stacking = boolean | 'from-sm';

const stackingClass = (stacked: Stacking) =>
  stacked === 'from-sm' ? 'items-baseline sm:flex-col sm:items-end' : stacked ? 'flex-col items-end' : 'items-baseline';

/** The price as every listing shows it: the sale price first, the list price struck through. */
const ProductPrice = ({ product, stacked = false }: { product: Product; stacked?: Stacking }) => {
  const { price, effectivePrice, hasSale } = priceTag(product);
  return hasSale ? (
    <span className={cn('inline-flex gap-x-2', stackingClass(stacked))}>
      <span className="font-medium text-brand">{formatAsDollars(effectivePrice)}</span>
      <span className="text-sm text-muted-foreground line-through">{formatAsDollars(price)}</span>
    </span>
  ) : (
    <span className="font-medium">{formatAsDollars(price)}</span>
  );
};

export default ProductPrice;

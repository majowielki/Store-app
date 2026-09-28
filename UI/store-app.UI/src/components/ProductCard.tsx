import { useRef } from 'react';
import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useFinishes } from '@/hooks/use-finishes';
import { useOpenProduct } from '@/hooks/use-open-product';
import { cn } from '@/lib/utils';
import { priceTag, type Product } from '@/utils';
import CompareToggle from './CompareToggle';
import ProductPrice from './ProductPrice';
import QuickView from './QuickView';
import ResponsiveImage from './ResponsiveImage';
import SaleBadge from './SaleBadge';
import StockBadge from './StockBadge';
import Swatch from './swatch/Swatch';
import { RatingLine } from './reviews/Stars';
import WishlistButton from './WishlistButton';

/** How many colours a listing shows before "+n". */
const SHOWN_COLORS = 5;

/** The first few colours a product comes in, as small swatches. */
export const ColorDots = ({ colors, className }: { colors: string[]; className?: string }) => {
  const { nameOf } = useFinishes();
  if (colors.length === 0) return null;
  const shown = colors.slice(0, SHOWN_COLORS);
  return (
    <div className={cn('flex items-center gap-1.5', className)} role="img" aria-label={`Colours: ${colors.map(nameOf).join(', ')}`}>
      {shown.map((color) => (
        <Swatch key={color} color={color} className="h-3.5 w-3.5" />
      ))}
      {colors.length > shown.length && <span className="text-xs text-muted-foreground">+{colors.length - shown.length}</span>}
    </div>
  );
};

interface ProductCardProps {
  product: Product;
  /**
   * The image travels to the product page (a view transition). Off for tiles on a product
   * page, whose own image already carries the transition's name.
   */
  transition?: boolean;
  className?: string;
}

/** A product tile of the grid listings (landing page, catalogue grid, related products). */
const ProductCard = ({ product, transition = true, className }: ProductCardProps) => {
  const { title, image, company, colors, newArrival } = product;
  const { hasSale, percent } = priceTag(product);
  const imageRef = useRef<HTMLImageElement>(null);
  const openProduct = useOpenProduct();

  return (
    <div className={cn('group relative', className)}>
      <Link
        to={`/products/${product.id}`}
        viewTransition={transition}
        onClick={() => openProduct(product, transition ? imageRef.current : null)}
        className="block rounded-2xl focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-4"
      >
        <div className="relative aspect-5/4 overflow-hidden rounded-2xl bg-muted">
          <ResponsiveImage
            ref={imageRef}
            src={image}
            size="card"
            placeholder
            alt={title}
            loading="lazy"
            decoding="async"
            className="h-full w-full object-cover transition-transform duration-700 ease-smooth group-hover:scale-[1.06]"
          />
          <div className="absolute left-3 top-3 flex gap-1.5">
            {hasSale && <SaleBadge percent={percent} />}
            {newArrival && (
              <span className="inline-flex items-center rounded-full bg-background/90 px-2.5 py-1 text-[11px] font-semibold leading-none tracking-wide backdrop-blur-sm">
                New
              </span>
            )}
            <StockBadge product={product} />
          </div>
          <span
            aria-hidden
            className="absolute bottom-3 right-3 grid h-11 w-11 translate-y-3 place-items-center rounded-full bg-background text-foreground opacity-0 shadow-lg transition-all duration-500 ease-smooth group-hover:translate-y-0 group-hover:opacity-100 group-focus-within:translate-y-0 group-focus-within:opacity-100"
          >
            <ArrowUpRight className="h-5 w-5 transition-transform duration-500 ease-smooth group-hover:rotate-45" />
          </span>
        </div>
        <div className="mt-4 flex items-start justify-between gap-4">
          <div className="min-w-0">
            <p className="eyebrow">{company}</p>
            <h3 className="mt-1.5 font-medium leading-snug">{title}</h3>
            <RatingLine average={product.ratingAverage} count={product.ratingCount} className="mt-1.5" />
          </div>
          <p className="shrink-0 text-right">
            <ProductPrice product={product} stacked />
          </p>
        </div>
        <ColorDots colors={colors} className="mt-3" />
      </Link>
      <CompareToggle product={product} className="-ml-2.5 mt-2" />
      {/* Over the picture but outside the link: a button may not sit inside a link */}
      <div className="pointer-events-none absolute inset-x-0 top-0 aspect-5/4">
        <WishlistButton product={product} className="pointer-events-auto absolute right-3 top-3" />
        <QuickView
          product={product}
          className="pointer-events-auto absolute bottom-3 left-3 translate-y-2 opacity-0 group-focus-within:translate-y-0 group-focus-within:opacity-100 group-hover:translate-y-0 group-hover:opacity-100 [@media(hover:none)]:translate-y-0 [@media(hover:none)]:opacity-100"
        />
      </div>
    </div>
  );
};

// Listings import the price with the card
export { ProductPrice };

export default ProductCard;

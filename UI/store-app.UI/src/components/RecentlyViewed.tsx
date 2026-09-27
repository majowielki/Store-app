import { Link } from 'react-router-dom';
import { useRecentlyViewed } from '@/features/recent/useRecentlyViewed';
import { cn } from '@/lib/utils';
import ProductPrice from './ProductPrice';

interface RecentlyViewedProps {
  /** The product on the page now, which is not shown in its own strip. */
  excludeId?: number;
  title?: string;
  className?: string;
  /** Called when a piece is chosen, e.g. to close the dialog the strip sits in. */
  onNavigate?: () => void;
}

/** "Recently viewed": the last pieces opened in this browser, in a row that scrolls sideways. */
const RecentlyViewed = ({ excludeId, title = 'Recently viewed', className, onNavigate }: RecentlyViewedProps) => {
  const { products } = useRecentlyViewed(excludeId);
  if (products.length === 0) return null;

  return (
    <section aria-label={title} className={cn('w-full', className)}>
      <h2 className="eyebrow">{title}</h2>
      <ul className="-mx-1 mt-4 flex snap-x gap-4 overflow-x-auto px-1 pb-2">
        {products.map((product) => (
          <li key={product.id} className="w-36 shrink-0 snap-start sm:w-44">
            <Link to={`/products/${product.id}`} onClick={onNavigate} className="group block rounded-xl text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
              <div className="aspect-square overflow-hidden rounded-xl bg-muted">
                <img src={product.image} alt="" loading="lazy" className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105" />
              </div>
              <p className="mt-2 line-clamp-2 text-sm font-medium leading-snug">{product.title}</p>
              <p className="text-sm">
                <ProductPrice product={product} />
              </p>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
};

export default RecentlyViewed;

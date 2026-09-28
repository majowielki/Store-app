import { Link, useLocation } from 'react-router-dom';
import ResponsiveImage from '@/components/ResponsiveImage';
import { ArrowRight, X } from 'lucide-react';
import { COMPARE_MAX, compareCleared, removedFromCompare } from '@/features/compare/compareSlice';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { useProductsById } from '@/hooks/use-products-by-id';
import { Button } from './ui/button';

/**
 * The pieces picked for comparison, in a bar at the bottom of the screen with the way to the
 * comparison. It stays out of the way on the comparison page itself.
 */
const CompareBar = () => {
  const dispatch = useAppDispatch();
  const ids = useAppSelector((state) => state.compare.productIds);
  const { products } = useProductsById(ids);
  const { pathname } = useLocation();

  if (products.length === 0 || pathname === '/compare') return null;

  return (
    <aside
      aria-label="Comparison"
      className="fixed inset-x-3 bottom-24 z-40 mx-auto flex max-w-2xl animate-fade-up items-center gap-3 rounded-2xl border bg-background/95 p-3 shadow-2xl backdrop-blur-sm md:bottom-6"
    >
      <ul className="flex flex-1 gap-2 overflow-x-auto pr-1.5 pt-1.5">
        {products.map((product) => (
          <li key={product.id} className="relative shrink-0">
            <ResponsiveImage size="thumbnail" src={product.image} alt={product.title} className="h-12 w-12 rounded-lg object-cover" />
            <button
              type="button"
              aria-label={`Remove ${product.title} from the comparison`}
              onClick={() => dispatch(removedFromCompare(product.id))}
              className="absolute -right-1.5 -top-1.5 grid h-5 w-5 place-items-center rounded-full bg-foreground text-background"
            >
              <X className="h-3 w-3" />
            </button>
          </li>
        ))}
        {Array.from({ length: COMPARE_MAX - products.length }, (_, index) => (
          <li key={`empty-${index}`} aria-hidden className="hidden h-12 w-12 shrink-0 rounded-lg border border-dashed sm:block" />
        ))}
      </ul>
      <Button type="button" variant="ghost" size="sm" onClick={() => dispatch(compareCleared())}>
        Clear
      </Button>
      <Button asChild size="sm" className="group">
        <Link to="/compare">
          Compare {products.length}
          <ArrowRight className="transition-transform group-hover:translate-x-0.5" />
        </Link>
      </Button>
    </aside>
  );
};

export default CompareBar;

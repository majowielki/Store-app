import { Scale } from 'lucide-react';
import { addedToCompare, COMPARE_MAX, removedFromCompare } from '@/features/compare/compareSlice';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { toast } from '@/hooks/use-toast';
import { cn } from '@/lib/utils';
import type { Product } from '@/utils';

/** "Compare": picks a product for the comparison, up to four; a fifth is refused with a word why. */
const CompareToggle = ({ product, className }: { product: Pick<Product, 'id' | 'title'>; className?: string }) => {
  const dispatch = useAppDispatch();
  const picked = useAppSelector((state) => state.compare.productIds);
  const on = picked.includes(product.id);

  const toggle = () => {
    if (on) {
      dispatch(removedFromCompare(product.id));
    } else if (picked.length >= COMPARE_MAX) {
      toast({ description: `You can compare up to ${COMPARE_MAX} pieces. Remove one to add ${product.title}.` });
    } else {
      dispatch(addedToCompare(product.id));
    }
  };

  return (
    <button
      type="button"
      aria-pressed={on}
      aria-label={on ? `Remove ${product.title} from the comparison` : `Compare ${product.title}`}
      onClick={toggle}
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium transition-colors focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring',
        on ? 'bg-foreground text-background' : 'text-muted-foreground hover:bg-secondary hover:text-foreground',
        className,
      )}
    >
      <Scale className="h-3.5 w-3.5" />
      {on ? 'Comparing' : 'Compare'}
    </button>
  );
};

export default CompareToggle;

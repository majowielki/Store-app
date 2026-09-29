import { Link } from 'react-router-dom';
import { ShoppingBag } from 'lucide-react';
import { useCart } from '@/features/cart/useCart';
import { cn } from '@/lib/utils';
import { countOf } from '@/utils';
import { Button } from './ui/button';

/** The count on the bag: re-keyed on every change, so it bumps when something goes in. */
export const CartCount = ({ count, className }: { count: number; className?: string }) =>
  count > 0 ? (
    <span
      key={count}
      aria-hidden
      className={cn(
        'absolute -right-0.5 -top-0.5 grid h-5 min-w-5 animate-bump place-items-center rounded-full bg-brand px-1 text-[10px] font-semibold leading-none text-brand-foreground',
        className,
      )}
    >
      {count}
    </span>
  ) : null;

const CartButton = () => {
  const { totalItems } = useCart();
  return (
    <Button asChild variant="ghost" size="icon" className="relative">
      <Link to="/cart" aria-label={`Cart, ${countOf(totalItems, 'item')}`}>
        <ShoppingBag className="h-[1.15rem]! w-[1.15rem]!" />
        <CartCount count={totalItems} />
      </Link>
    </Button>
  );
};
export default CartButton;

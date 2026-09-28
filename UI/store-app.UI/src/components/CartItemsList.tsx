import { Link } from 'react-router-dom';
import ResponsiveImage from '@/components/ResponsiveImage';
import { Trash2 } from 'lucide-react';
import { useCartActions, type CartLine } from '@/features/cart/useCart';
import { toast } from '@/hooks/use-toast';
import { formatAsDollars } from '@/utils';
import SelectProductAmount, { Mode } from './SelectProductAmount';
import FinishLabel from './swatch/FinishLabel';
import { Button } from './ui/button';

/** One line of the cart: the product, its colour and price, the amount and the line's total. */
const CartLineRow = ({ line }: { line: CartLine }) => {
  const { setQuantity, remove } = useCartActions();

  // A refused change has been reported by the error middleware; nothing to add here
  const removeLine = async () => {
    try {
      await remove(line);
      toast({ description: 'Item removed from the cart' });
    } catch {
      // reported
    }
  };

  const changeQuantity = async (value: number) => {
    try {
      await setQuantity(line, value);
      toast({ description: 'Amount updated' });
    } catch {
      // reported
    }
  };

  return (
    <li data-testid="cart-line" className="grid animate-fade-up grid-cols-[6rem_1fr] gap-5 py-6 sm:grid-cols-[8.5rem_1fr_auto] sm:gap-6">
      <Link to={`/products/${line.productId}`} className="group block aspect-5/4 overflow-hidden rounded-xl bg-muted">
        {line.image && (
          <ResponsiveImage size="tile" src={line.image} alt={line.title} className="h-full w-full object-cover transition-transform duration-700 ease-smooth group-hover:scale-105" />
        )}
      </Link>
      <div className="min-w-0">
        <p className="eyebrow">{line.company}</p>
        <h3 className="mt-1 font-medium">{line.title}</h3>
        <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
          <FinishLabel color={line.color} />
          <span>Price: {formatAsDollars(line.unitPrice)}</span>
        </div>
        <div className="mt-4 flex items-center gap-2">
          <SelectProductAmount amount={line.quantity} setAmount={changeQuantity} mode={Mode.CartItem} />
          <Button variant="ghost" size="sm" className="h-9 capitalize text-muted-foreground hover:text-destructive" onClick={removeLine}>
            <Trash2 />
            remove
          </Button>
        </div>
      </div>
      <div className="col-start-2 sm:col-start-auto sm:text-right">
        <p className="sr-only">Total price:</p>
        <p className="text-lg font-medium tabular-nums">{formatAsDollars(line.lineTotal)}</p>
      </div>
    </li>
  );
};

const CartItemsList = ({ lines }: { lines: CartLine[] }) => (
  <ul className="divide-y border-y">
    {lines.map((line) => (
      <CartLineRow key={line.key} line={line} />
    ))}
  </ul>
);
export default CartItemsList;

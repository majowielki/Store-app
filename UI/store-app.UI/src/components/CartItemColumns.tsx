import { formatAsDollars } from '@/utils';
import { useCartActions, type CartLine } from '@/features/cart/useCart';
import { toast } from '@/hooks/use-toast';
import { Button } from './ui/button';
import SelectProductAmount from './SelectProductAmount';
import { Mode } from './SelectProductAmount';

interface FirstColumnProps {
  image: string;
  title: string;
}

interface SecondColumnProps {
  title: string;
  company: string;
  productColor: string;
  price: number;
}

export const FirstColumn = ({ title, image }: FirstColumnProps) => (
  <div className="w-24 h-18 sm:w-32 sm:h-24 aspect-[4/3] bg-gray-100 rounded-lg overflow-hidden flex items-center justify-center">
    <img src={image} alt={title} className="w-full h-full object-cover" style={{ aspectRatio: '4/3' }} />
  </div>
);

export const SecondColumn = ({ title, company, productColor, price }: SecondColumnProps) => (
  <div className="sm:ml-4 md:ml-12 sm:w-48">
    <h3 className="capitalize font-medium">{title}</h3>
    <h4 className="mt-3 capitalize text-sm">{company}</h4>
    <p className="mt-4 text-sm capitalize flex items-center gap-x-2">
      color :{' '}
      <span
        style={{
          width: '15px',
          height: '15px',
          borderRadius: '50%',
          background: productColor,
        }}
      ></span>
    </p>
    <p className="mt-4 text-sm">Price: {formatAsDollars(price)}</p>
  </div>
);

export const ThirdColumn = ({ line }: { line: CartLine }) => {
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
    <div>
      <SelectProductAmount amount={line.quantity} setAmount={changeQuantity} mode={Mode.CartItem} />
      <Button variant="link" className="-ml-4" onClick={removeLine}>
        remove
      </Button>
    </div>
  );
};

export const FourthColumn = ({ lineTotal }: { lineTotal: number }) => (
  <div className="sm:ml-auto">
    <p className="font-medium">Total price:</p>
    <p>{formatAsDollars(lineTotal)}</p>
  </div>
);

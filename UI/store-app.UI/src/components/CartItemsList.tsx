import type { CartLine } from '@/features/cart/useCart';
import { Card } from './ui/card';
import { FirstColumn, SecondColumn, ThirdColumn, FourthColumn } from './CartItemColumns';

const CartItemsList = ({ lines }: { lines: CartLine[] }) => (
  <div>
    {lines.map((line) => (
      <Card key={line.key} data-testid="cart-line" className="flex flex-col gap-y-4 sm:flex-row flex-wrap p-6 mb-8">
        <FirstColumn image={line.image} title={line.title} />
        <SecondColumn title={line.title} company={line.company} productColor={line.color} price={line.unitPrice} />
        <ThirdColumn line={line} />
        <FourthColumn lineTotal={line.lineTotal} />
      </Card>
    ))}
  </div>
);
export default CartItemsList;

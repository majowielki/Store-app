import { useGetHasOrdersQuery } from '@/api/orders';
import { Card, CardTitle } from '@/components/ui/card';
import { previewTotals } from '@/features/cart/pricing';
import { useCart } from '@/features/cart/useCart';
import { useAppSelector } from '@/hooks';
import { formatAsDollars } from '@/utils';
import { Separator } from './ui/separator';

interface CartTotalRowProps {
  label: string;
  amount: number;
  lastRow?: boolean;
  isDiscount?: boolean;
}

const CartTotalRow = ({ label, amount, lastRow, isDiscount }: CartTotalRowProps) => (
  <>
    <p className={`flex justify-between text-sm ${isDiscount ? 'text-green-700' : ''}`}>
      <span>{label}</span>
      <span>
        {isDiscount ? '-' : ''}
        {formatAsDollars(Math.abs(amount))}
      </span>
    </p>
    {lastRow ? null : <Separator className="my-2" />}
  </>
);

/** A preview of the order's amounts; the order service prices the order itself when it is placed. */
const CartTotals = () => {
  const user = useAppSelector((state) => state.session.user);
  const { subtotal } = useCart();
  // Only a signed-in customer can be on their first order; a visitor sees the plain total
  const { data: orders } = useGetHasOrdersQuery(undefined, { skip: !user });
  const firstOrder = !!user && orders?.ordersCount === 0;
  const totals = previewTotals(subtotal, firstOrder);

  return (
    <Card className="p-8 bg-muted">
      <CartTotalRow label="Subtotal" amount={totals.subtotal} />
      {totals.discount > 0 && <CartTotalRow label="First order discount" amount={-totals.discount} isDiscount />}
      <CartTotalRow label="Delivery" amount={totals.deliveryFee} />
      <CardTitle className="mt-8">
        <CartTotalRow label="Order Total" amount={totals.total} lastRow />
      </CardTitle>
    </Card>
  );
};
export default CartTotals;

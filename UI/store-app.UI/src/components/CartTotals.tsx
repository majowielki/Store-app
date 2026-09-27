import { useGetHasOrdersQuery, useGetPricingRulesQuery } from '@/api/orders';
import { previewTotals } from '@/features/cart/pricing';
import { useCart } from '@/features/cart/useCart';
import { useDiscountCode } from '@/features/cart/useDiscountCode';
import { useAppSelector } from '@/hooks';
import { cn } from '@/lib/utils';
import { formatAsDollars } from '@/utils';

interface CartTotalRowProps {
  label: string;
  amount: number;
  total?: boolean;
  isDiscount?: boolean;
}

const CartTotalRow = ({ label, amount, total, isDiscount }: CartTotalRowProps) => (
  <p
    className={cn(
      'flex items-baseline justify-between',
      total ? 'mt-3 border-t pt-4 text-lg font-medium' : 'py-1.5 text-sm',
      isDiscount ? 'text-success' : !total && 'text-muted-foreground',
    )}
  >
    <span>{label}</span>
    <span className={cn('tabular-nums', !total && !isDiscount && 'text-foreground')}>
      {isDiscount ? '-' : ''}
      {formatAsDollars(Math.abs(amount))}
    </span>
  </p>
);

/**
 * A preview of the order's amounts, computed the way the order service will compute them,
 * with the rules it publishes; the order itself is priced by the server when it is placed.
 */
const CartTotals = ({ className }: { className?: string }) => {
  const user = useAppSelector((state) => state.session.user);
  const { subtotal } = useCart();
  const { data: rules, isError } = useGetPricingRulesQuery();
  // Only a signed-in customer can be on their first order; a visitor sees the plain total
  const { data: orders } = useGetHasOrdersQuery(undefined, { skip: !user });
  const firstOrder = !!user && orders?.ordersCount === 0;
  const { check } = useDiscountCode();

  if (!rules) {
    return (
      <div className={className}>
        <CartTotalRow label="Subtotal" amount={subtotal} />
        <p className="mt-2 text-sm text-muted-foreground">
          {isError ? 'Delivery and discounts will be shown at checkout.' : 'Calculating delivery and discounts…'}
        </p>
      </div>
    );
  }

  const totals = previewTotals(subtotal, firstOrder, rules, check?.discountAmount ?? 0);
  return (
    <div className={className}>
      <CartTotalRow label="Subtotal" amount={totals.subtotal} />
      {totals.discount > 0 && (
        <CartTotalRow label={totals.discountReason === 'code' ? `Code ${check?.code}` : 'First order discount'} amount={-totals.discount} isDiscount />
      )}
      {check && totals.discountReason === 'first-order' && (
        <p className="pb-1.5 text-xs text-muted-foreground">
          Your first-order discount is larger, so {check.code} is kept for another order.
        </p>
      )}
      <CartTotalRow label="Delivery" amount={totals.deliveryFee} />
      <CartTotalRow label="Order Total" amount={totals.total} total />
    </div>
  );
};
export default CartTotals;

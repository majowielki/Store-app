import { Truck } from 'lucide-react';
import { useGetPricingRulesQuery } from '@/api/orders';
import { formatAsDollars } from '@/utils';

/** How far the cart is from free delivery, with the order service's threshold. */
const FreeDeliveryProgress = ({ subtotal }: { subtotal: number }) => {
  const { data: rules } = useGetPricingRulesQuery();
  if (!rules || rules.freeDeliveryThreshold <= 0) return null;

  const remaining = rules.freeDeliveryThreshold - subtotal;
  const progress = Math.min(100, (subtotal / rules.freeDeliveryThreshold) * 100);
  return (
    <div className="mt-6 rounded-2xl bg-secondary/70 p-4">
      <p className="flex items-center gap-2 text-sm">
        <Truck className="h-4 w-4 shrink-0" />
        {remaining > 0 ? (
          <span>
            Add <strong className="font-medium">{formatAsDollars(remaining)}</strong> more for free delivery
          </span>
        ) : (
          <span>Your order ships for free</span>
        )}
      </p>
      <div className="mt-3 h-1.5 overflow-hidden rounded-full bg-border" role="progressbar" aria-valuemin={0} aria-valuemax={100} aria-valuenow={Math.round(progress)} aria-label="Progress to free delivery">
        <div className="h-full rounded-full bg-brand transition-[width] duration-700 ease-smooth" style={{ width: `${progress}%` }} />
      </div>
    </div>
  );
};

export default FreeDeliveryProgress;

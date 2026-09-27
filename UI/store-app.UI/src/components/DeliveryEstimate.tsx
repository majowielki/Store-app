import { Truck } from 'lucide-react';
import { useGetPricingRulesQuery } from '@/api/orders';
import { cn } from '@/lib/utils';
import { formatDayRange } from '@/utils';

/**
 * "Arrives Wed, Sep 30 – Fri, Oct 2": when an order placed now should arrive, as the order
 * service estimates it. Nothing is shown until the estimate is known.
 */
const DeliveryEstimate = ({ className }: { className?: string }) => {
  const { data: rules } = useGetPricingRulesQuery();
  if (!rules) return null;
  return (
    <p className={cn('flex items-center gap-2 text-sm', className)}>
      <Truck className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
      <span>
        Arrives <span className="font-medium">{formatDayRange(rules.deliveryFrom, rules.deliveryTo)}</span>
      </span>
    </p>
  );
};

export default DeliveryEstimate;

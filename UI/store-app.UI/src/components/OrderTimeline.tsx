import { Check, X } from 'lucide-react';
import { Badge, type BadgeProps } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { formatDateTime, formatDayRange, type Order, type OrderStatus } from '@/utils';

const labels: Record<OrderStatus, string> = {
  Placed: 'Order placed',
  Paid: 'Payment received',
  Shipped: 'Shipped',
  Cancelled: 'Cancelled',
};

const journey: OrderStatus[] = ['Placed', 'Paid', 'Shipped'];

interface Step {
  status: OrderStatus;
  /** When the order entered this status; none for a step still ahead. */
  at?: string;
}

/**
 * The steps of an order. A cancelled order shows the steps it went through and where it
 * stopped; any other order the whole way, placed → paid → shipped, with the steps ahead
 * greyed out and the delivery window promised at checkout.
 */
const OrderTimeline = ({ order, className }: { order: Order; className?: string }) => {
  const reached = new Map(order.statusHistory.map((change) => [change.status as OrderStatus, change.changedAt]));
  const cancelled = reached.has('Cancelled');
  const steps: Step[] = cancelled
    ? order.statusHistory.map((change) => ({ status: change.status as OrderStatus, at: change.changedAt }))
    : journey.map((status) => ({ status, at: reached.get(status) }));
  const current = steps.filter((step) => step.at).at(-1)?.status;

  return (
    <div className={cn('rounded-2xl border bg-card p-6', className)}>
      <h2 className="eyebrow mb-5">Progress</h2>
      <ol aria-label="Order progress" className="relative grid gap-5">
        {steps.map((step, index) => {
          const done = step.at !== undefined;
          const stopped = step.status === 'Cancelled';
          return (
            <li key={step.status} aria-current={step.status === current ? 'step' : undefined} className="relative flex gap-4">
              {index < steps.length - 1 && (
                <span aria-hidden className={cn('absolute left-[0.8rem] top-7 h-[calc(100%-0.5rem)] w-px', done ? 'bg-foreground/40' : 'bg-border')} />
              )}
              <span
                aria-hidden
                className={cn(
                  'relative grid h-[1.65rem] w-[1.65rem] shrink-0 place-items-center rounded-full border',
                  stopped ? 'border-destructive bg-destructive text-destructive-foreground' : done ? 'border-foreground bg-foreground text-background' : 'bg-background',
                )}
              >
                {stopped ? <X className="h-3.5 w-3.5" /> : done ? <Check className="h-3.5 w-3.5" /> : null}
              </span>
              <div className="min-w-0 pt-0.5">
                <p className={cn('text-sm font-medium', !done && 'text-muted-foreground')}>{labels[step.status]}</p>
                <p className="text-xs text-muted-foreground">{done ? formatDateTime(step.at!) : 'Not yet'}</p>
              </div>
            </li>
          );
        })}
      </ol>
      {!cancelled && order.deliveryFrom && order.deliveryTo && (
        <p className="mt-6 border-t pt-4 text-sm">
          <span className="text-muted-foreground">{current === 'Shipped' ? 'On its way, arriving' : 'Estimated delivery'}: </span>
          <span className="font-medium">{formatDayRange(order.deliveryFrom, order.deliveryTo)}</span>
        </p>
      )}
    </div>
  );
};

export default OrderTimeline;

const badgeVariant: Record<OrderStatus, BadgeProps['variant']> = {
  Placed: 'outline',
  Paid: 'secondary',
  Shipped: 'default',
  Cancelled: 'destructive',
};

/** The order's status in a list of orders. */
export const OrderStatusBadge = ({ status }: { status: string }) => (
  <Badge variant={badgeVariant[status as OrderStatus] ?? 'outline'} className="whitespace-nowrap font-medium">
    {status}
  </Badge>
);

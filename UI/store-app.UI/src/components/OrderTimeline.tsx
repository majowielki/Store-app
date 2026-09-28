import { Check, X } from 'lucide-react';
import { Badge, type BadgeProps } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { formatDateTime, formatDayRange, type Order, type OrderStatus } from '@/utils';

const labels: Record<OrderStatus, string> = {
  Placed: 'Order placed',
  AwaitingPayment: 'Awaiting payment',
  Paid: 'Payment received',
  Shipped: 'Shipped',
  Cancelled: 'Cancelled',
  Refunded: 'Refunded',
};

/** Why an order was cancelled, in the customer's words. */
const cancellationReasons: Record<string, string> = {
  'out-of-stock': 'Part of the order had sold out by the time it reached our warehouse.',
  'payment-timed-out': 'The payment did not arrive in time, so the pieces went back on sale.',
  'cancelled-by-administrator': 'The order was cancelled by the shop.',
};

const journey: OrderStatus[] = ['Placed', 'AwaitingPayment', 'Paid', 'Shipped'];

interface Step {
  status: OrderStatus;
  /** When the order entered this status; none for a step still ahead. */
  at?: string;
}

/** "Visa •••• 4242" for the card that paid, when the order knows it. */
const paidWith = (order: Order): string | null =>
  order.cardBrand && order.cardLast4 ? `${order.cardBrand.charAt(0).toUpperCase()}${order.cardBrand.slice(1)} •••• ${order.cardLast4}` : null;

/**
 * The steps of an order. A cancelled order shows the steps it went through, where it stopped,
 * why, and its refund; any other order the whole way, placed → awaiting payment → paid →
 * shipped, with the steps ahead greyed out and the delivery window promised at checkout.
 */
const OrderTimeline = ({ order, className }: { order: Order; className?: string }) => {
  const reached = new Map(order.statusHistory.map((change) => [change.status as OrderStatus, change.changedAt]));
  const cancelled = reached.has('Cancelled');
  const steps: Step[] = cancelled
    ? order.statusHistory.map((change) => ({ status: change.status as OrderStatus, at: change.changedAt }))
    : journey.map((status) => ({ status, at: reached.get(status) }));
  const current = steps.filter((step) => step.at).at(-1)?.status;
  const card = paidWith(order);

  const detail = (step: Step): string => {
    if (!step.at) return step.status === 'AwaitingPayment' && current === 'Placed' ? 'Reserving your pieces' : 'Not yet';
    const at = formatDateTime(step.at);
    if (step.status === 'Paid' && card) return `${at} · ${card}`;
    if (step.status === 'AwaitingPayment' && current === 'AwaitingPayment' && order.paymentDueAt) return `Pay by ${formatDateTime(order.paymentDueAt)}`;
    return at;
  };

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
                <p className="text-xs text-muted-foreground">{detail(step)}</p>
              </div>
            </li>
          );
        })}
      </ol>
      {cancelled && (
        <p className="mt-6 border-t pt-4 text-sm text-muted-foreground">
          {cancellationReasons[order.cancellationReason ?? ''] ?? 'The order was cancelled.'}
          {current === 'Refunded' && ' The payment has been returned to your card.'}
          {current === 'Cancelled' && card && ' The payment is on its way back to your card.'}
        </p>
      )}
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
  AwaitingPayment: 'outline',
  Paid: 'secondary',
  Shipped: 'default',
  Cancelled: 'destructive',
  Refunded: 'secondary',
};

const badgeText: Record<OrderStatus, string> = {
  Placed: 'Placed',
  AwaitingPayment: 'Awaiting payment',
  Paid: 'Paid',
  Shipped: 'Shipped',
  Cancelled: 'Cancelled',
  Refunded: 'Refunded',
};

/** The order's status in a list of orders. */
export const OrderStatusBadge = ({ status }: { status: string }) => (
  <Badge variant={badgeVariant[status as OrderStatus] ?? 'outline'} className="whitespace-nowrap font-medium">
    {badgeText[status as OrderStatus] ?? status}
  </Badge>
);

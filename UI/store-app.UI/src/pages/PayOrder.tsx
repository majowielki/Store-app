import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Check, Loader2, X } from 'lucide-react';
import { ordersApi, useGetOrderQuery } from '@/api/orders';
import { HttpStatus, isApiError } from '@/api/problem';
import type { Order } from '@/api/types';
import CheckoutSteps from '@/components/CheckoutSteps';
import Loading from '@/components/Loading';
import CardForm from '@/components/payment/CardForm';
import { describeCard } from '@/components/payment/cards';
import PaymentCountdown from '@/components/payment/PaymentCountdown';
import FinishLabel from '@/components/swatch/FinishLabel';
import { Button } from '@/components/ui/button';
import { formatAsDollars, formatDayRange } from '@/utils';

/** How long confirming may take before the page says it is taking longer than usual. */
const SLOW_AFTER_MS = 20_000;

/** How often the order is asked again while something is on its way. */
const POLL_INTERVAL_MS = 1_000;

const cancellationText: Record<string, string> = {
  'out-of-stock': 'Part of the order had sold out by the time it reached our warehouse, so nothing was charged.',
  'payment-timed-out': 'The payment did not arrive in time, so the pieces went back on sale.',
  'cancelled-by-administrator': 'The order was cancelled by the shop.',
};

const Heading = ({ step, eyebrow, title }: { step: number; eyebrow: string; title: string }) => (
  <header className="animate-fade-up">
    <CheckoutSteps current={step} />
    <p className="eyebrow mt-8">{eyebrow}</p>
    <h1 className="display mt-3 text-5xl md:text-7xl">{title}</h1>
  </header>
);

/** A step the page waits through: the stock being reserved, the payment being confirmed. */
const Waiting = ({ order, title, text, slow }: { order: Order; title: string; text: string; slow?: boolean }) => (
  <>
    <Heading step={2} eyebrow={`Order #${order.id}`} title={title} />
    <div role="status" className="mt-10 flex items-start gap-4 rounded-3xl border bg-card p-6 md:p-10">
      <Loader2 className="mt-0.5 h-5 w-5 shrink-0 animate-spin" aria-hidden />
      <div>
        <p>{text}</p>
        {slow && (
          <p className="mt-3 text-sm text-muted-foreground">
            This is taking longer than usual. You can leave the page - the order will show the payment as soon as it arrives.{' '}
            <Link to={`/orders/${order.id}`} className="link-underline text-foreground">
              See your order
            </Link>
          </p>
        )}
      </div>
    </div>
  </>
);

/** The order is paid: the last step of the purchase. */
const Paid = ({ order }: { order: Order }) => (
  <>
    <Heading step={4} eyebrow={`Order #${order.id}`} title="Thank you" />
    <div role="status" className="mt-10 grid gap-6 rounded-3xl border bg-card p-6 md:p-10">
      <p className="flex items-center gap-3 text-lg">
        <span className="grid h-8 w-8 place-items-center rounded-full bg-foreground text-background">
          <Check className="h-4 w-4" aria-hidden />
        </span>
        Payment received
      </p>
      <p className="text-muted-foreground">
        {formatAsDollars(order.total)} paid with {describeCard(order.cardBrand, order.cardLast4)}. We will write to {order.userEmail} when your pieces
        are on their way
        {order.deliveryFrom && order.deliveryTo ? `; they should arrive ${formatDayRange(order.deliveryFrom, order.deliveryTo)}.` : '.'}
      </p>
      <div className="flex flex-wrap gap-3">
        <Button asChild>
          <Link to={`/orders/${order.id}`}>See your order</Link>
        </Button>
        <Button asChild variant="outline">
          <Link to="/products">Continue shopping</Link>
        </Button>
      </div>
    </div>
  </>
);

/** The order can no longer be paid: cancelled (out of stock, too late, by the shop) or refunded. */
const Closed = ({ order }: { order: Order }) => (
  <>
    <Heading step={2} eyebrow={`Order #${order.id}`} title="This order was cancelled" />
    <div role="status" className="mt-10 grid gap-6 rounded-3xl border bg-card p-6 md:p-10">
      <p className="flex items-center gap-3">
        <span className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-destructive text-destructive-foreground">
          <X className="h-4 w-4" aria-hidden />
        </span>
        {cancellationText[order.cancellationReason ?? ''] ?? 'The order was cancelled.'}
      </p>
      {order.cardLast4 && <p className="text-sm text-muted-foreground">The payment has been, or is being, returned to {describeCard(order.cardBrand, order.cardLast4)}.</p>}
      <div className="flex flex-wrap gap-3">
        <Button asChild>
          <Link to="/products">Back to the shop</Link>
        </Button>
        <Button asChild variant="outline">
          <Link to={`/orders/${order.id}`}>See the order</Link>
        </Button>
      </div>
    </div>
  </>
);

/**
 * The payment step of the purchase, at /orders/:id/pay. It follows the order: while its pieces
 * are being reserved it waits, once they are held it takes a card before the deadline, and after
 * the card went through it waits for the shop to hear it from the payment service (a webhook, a
 * second or two) before it says thank you.
 */
const PayOrder = () => {
  const orderId = Number(useParams<{ id: string }>().id);
  const [confirming, setConfirming] = useState(false);
  const [slow, setSlow] = useState(false);
  const [expired, setExpired] = useState(false);

  // Asked again every second while something is on its way: the reservation, the payment, the cancellation after the deadline
  const { data: seen } = ordersApi.endpoints.getOrder.useQueryState(orderId);
  const waiting = !seen || seen.status === 'Placed' || (seen.status === 'AwaitingPayment' && (confirming || expired));
  const { data: order, isLoading, error, refetch } = useGetOrderQuery(orderId, { pollingInterval: waiting ? POLL_INTERVAL_MS : 0, refetchOnMountOrArgChange: true });

  useEffect(() => {
    if (!confirming) return;
    const timer = window.setTimeout(() => setSlow(true), SLOW_AFTER_MS);
    return () => window.clearTimeout(timer);
  }, [confirming]);

  // Each step replaces the page; the next one starts at its top, not where the form was scrolled to
  const step = order ? `${order.status}:${confirming}` : 'loading';
  useEffect(() => {
    window.scrollTo?.({ top: 0 });
  }, [step]);

  const onPaid = useCallback(() => setConfirming(true), []);
  const onClosed = useCallback(() => void refetch(), [refetch]);
  const onExpired = useCallback(() => setExpired(true), []);

  if (isLoading) return <Loading />;
  if (!order) {
    return (
      <div className="grid place-items-center py-24 text-center">
        <h1 className="display text-5xl">{isApiError(error) && error.status === HttpStatus.NotFound ? 'Order not found' : 'Order unavailable'}</h1>
        <Button asChild variant="outline" className="mt-8">
          <Link to="/orders">Your orders</Link>
        </Button>
      </div>
    );
  }

  switch (order.status) {
    case 'Paid':
    case 'Shipped':
      return <Paid order={order} />;
    case 'Cancelled':
    case 'Refunded':
      return <Closed order={order} />;
    case 'Placed':
      return <Waiting order={order} title="Reserving your pieces" text="We are setting your pieces aside in the warehouse. This takes a second or two." />;
  }

  if (confirming) {
    return <Waiting order={order} title="Confirming your payment" text="Your bank said yes. We are waiting for the payment to be confirmed." slow={slow} />;
  }

  return (
    <>
      <Heading step={2} eyebrow={`Order #${order.id}`} title="Payment" />
      <div className="mt-10 grid items-start gap-10 lg:grid-cols-12 lg:gap-12">
        <div className="min-w-0 rounded-3xl border bg-card p-6 md:p-10 lg:col-span-7">
          <h2 className="display text-3xl">Card details</h2>
          {order.paymentDueAt && <PaymentCountdown dueAt={order.paymentDueAt} onExpired={onExpired} className="mb-6 mt-3" />}
          <CardForm order={order} onPaid={onPaid} onClosed={onClosed} />
        </div>
        <aside className="min-w-0 rounded-3xl bg-secondary/60 p-6 md:p-8 lg:sticky lg:top-24 lg:col-span-5">
          <h2 className="display text-3xl">Your order</h2>
          <ul className="mt-6 grid gap-4">
            {order.orderItems.map((line) => (
              <li key={line.id} className="flex items-center gap-4">
                <div className="h-16 w-20 shrink-0 overflow-hidden rounded-xl bg-muted">
                  {line.productImage && <img src={line.productImage} alt="" className="h-full w-full object-cover" />}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">{line.productTitle}</p>
                  <p className="text-xs text-muted-foreground">
                    <FinishLabel color={line.color} /> · {line.quantity} ×
                  </p>
                </div>
                <span className="text-sm tabular-nums">{formatAsDollars(line.lineTotal)}</span>
              </li>
            ))}
          </ul>
          <p className="mt-6 flex items-baseline justify-between border-t pt-4 text-base font-medium">
            <span>To pay</span>
            <span className="tabular-nums">{formatAsDollars(order.total)}</span>
          </p>
        </aside>
      </div>
    </>
  );
};

export default PayOrder;

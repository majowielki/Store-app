import { Link } from 'react-router-dom';
import { Check } from 'lucide-react';
import { CheckoutForm, Loading, CartTotals } from '@/components';
import { Button } from '@/components/ui/button';
import { useCart } from '@/features/cart/useCart';
import { cn } from '@/lib/utils';
import { formatAsDollars } from '@/utils';

const steps = ['Cart', 'Delivery', 'Done'];

/** Where the customer is in the purchase: the cart behind them, the delivery details now. */
const Steps = ({ current }: { current: number }) => (
  <ol className="flex items-center gap-3 text-xs">
    {steps.map((step, index) => (
      <li key={step} className="flex items-center gap-3">
        <span
          className={cn(
            'grid h-6 w-6 place-items-center rounded-full border text-[10px] font-medium',
            index < current && 'border-foreground bg-foreground text-background',
            index === current && 'border-foreground',
            index > current && 'text-muted-foreground',
          )}
        >
          {index < current ? <Check className="h-3 w-3" /> : index + 1}
        </span>
        <span className={index > current ? 'text-muted-foreground' : 'font-medium'}>{step}</span>
        {index < steps.length - 1 && <span className="h-px w-8 bg-border" />}
      </li>
    ))}
  </ol>
);

const Checkout = () => {
  const { lines, isLoading } = useCart();

  if (isLoading) return <Loading />;
  if (lines.length === 0) {
    return (
      <div className="grid place-items-center py-24 text-center">
        <h1 className="display text-5xl">Your cart is empty</h1>
        <Button asChild variant="outline" className="mt-8">
          <Link to="/products">Back to the shop</Link>
        </Button>
      </div>
    );
  }

  return (
    <>
      <header className="animate-fade-up">
        <Steps current={1} />
        <h1 className="display mt-8 text-5xl md:text-7xl">Place your order</h1>
      </header>
      <div className="mt-10 grid items-start gap-10 lg:grid-cols-12 lg:gap-12">
        <div className="rounded-3xl border bg-card p-6 md:p-10 lg:col-span-7">
          <CheckoutForm />
        </div>
        <aside className="rounded-3xl bg-secondary/60 p-6 md:p-8 lg:sticky lg:top-24 lg:col-span-5">
          <h2 className="display text-3xl">In your bag</h2>
          <ul className="mt-6 grid gap-4">
            {lines.map((line) => (
              <li key={line.key} className="flex items-center gap-4">
                <div className="relative h-16 w-20 shrink-0">
                  <div className="h-full w-full overflow-hidden rounded-xl bg-muted">
                    {line.image && <img src={line.image} alt="" className="h-full w-full object-cover" />}
                  </div>
                  <span className="absolute -right-2 -top-2 grid h-5 min-w-5 place-items-center rounded-full bg-foreground px-1 text-[10px] font-semibold text-background">
                    {line.quantity}
                  </span>
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">{line.title}</p>
                  <p className="text-xs capitalize text-muted-foreground">{line.color}</p>
                </div>
                <span className="text-sm tabular-nums">{formatAsDollars(line.lineTotal)}</span>
              </li>
            ))}
          </ul>
          <CartTotals className="mt-6 border-t pt-4" />
        </aside>
      </div>
    </>
  );
};
export default Checkout;

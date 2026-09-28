import { Link } from 'react-router-dom';
import ResponsiveImage from '@/components/ResponsiveImage';
import { CheckoutForm, Loading, CartTotals } from '@/components';
import CheckoutSteps from '@/components/CheckoutSteps';
import DeliveryEstimate from '@/components/DeliveryEstimate';
import FinishLabel from '@/components/swatch/FinishLabel';
import { Button } from '@/components/ui/button';
import { useCart } from '@/features/cart/useCart';
import { usePageMeta } from '@/seo';
import { formatAsDollars } from '@/utils';

const Checkout = () => {
  usePageMeta({ title: 'Checkout', noindex: true });
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
        <CheckoutSteps current={1} />
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
                    {line.image && <ResponsiveImage size="thumbnail" src={line.image} alt="" className="h-full w-full object-cover" />}
                  </div>
                  <span className="absolute -right-2 -top-2 grid h-5 min-w-5 place-items-center rounded-full bg-foreground px-1 text-[10px] font-semibold text-background">
                    {line.quantity}
                  </span>
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">{line.title}</p>
                  <FinishLabel color={line.color} className="text-xs text-muted-foreground" />
                </div>
                <span className="text-sm tabular-nums">{formatAsDollars(line.lineTotal)}</span>
              </li>
            ))}
          </ul>
          <CartTotals className="mt-6 border-t pt-4" />
          <DeliveryEstimate className="mt-4 border-t pt-4" />
        </aside>
      </div>
    </>
  );
};
export default Checkout;

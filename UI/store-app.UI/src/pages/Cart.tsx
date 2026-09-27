import { Link } from 'react-router-dom';
import { ArrowRight, Lock, ShoppingBag } from 'lucide-react';
import { CartItemsList, Loading, CartTotals } from '@/components';
import DiscountCodeField from '@/components/DiscountCodeField';
import FreeDeliveryProgress from '@/components/FreeDeliveryProgress';
import { Button } from '@/components/ui/button';
import { useCart } from '@/features/cart/useCart';
import { useAppSelector } from '@/hooks';

const Cart = () => {
  const user = useAppSelector((state) => state.session.user);
  const { lines, isLoading, totalItems, subtotal } = useCart();

  if (isLoading) return <Loading />;
  if (lines.length === 0) {
    return (
      <div className="grid place-items-center py-16 text-center md:py-24">
        <span className="grid h-20 w-20 animate-fade-up place-items-center rounded-full bg-secondary">
          <ShoppingBag className="h-8 w-8" />
        </span>
        <h1 className="display mt-8 animate-fade-up text-5xl [animation-delay:80ms] md:text-6xl">Your cart is empty</h1>
        <p className="mt-4 max-w-sm animate-fade-up text-muted-foreground [animation-delay:160ms]">
          Nothing here yet — the shop is full of things that would fit right in.
        </p>
        <Button asChild size="lg" className="group mt-10 animate-fade-up [animation-delay:240ms]">
          <Link to="/products">
            Start shopping
            <ArrowRight className="transition-transform duration-300 group-hover:translate-x-1" />
          </Link>
        </Button>
      </div>
    );
  }

  return (
    <>
      <header className="animate-fade-up">
        <p className="eyebrow">Your bag</p>
        <div className="mt-3 flex flex-wrap items-end justify-between gap-4">
          <h1 className="display text-5xl md:text-7xl">Shopping Cart</h1>
          <p className="text-sm text-muted-foreground">
            {totalItems} {totalItems === 1 ? 'piece' : 'pieces'}
          </p>
        </div>
      </header>
      <div className="mt-10 grid items-start gap-12 lg:grid-cols-12">
        <div className="lg:col-span-7">
          <CartItemsList lines={lines} />
          <Link to="/products" className="group mt-6 inline-flex items-center gap-2 text-sm font-medium">
            <ArrowRight className="h-4 w-4 rotate-180 transition-transform duration-300 group-hover:-translate-x-1" />
            <span className="link-underline">Continue shopping</span>
          </Link>
        </div>
        <aside className="lg:sticky lg:top-24 lg:col-span-5">
          <div className="rounded-3xl border bg-card p-6 md:p-8">
            <h2 className="display text-3xl">Order summary</h2>
            <FreeDeliveryProgress subtotal={subtotal} />
            <DiscountCodeField className="mt-6" />
            <CartTotals className="mt-6" />
            <Button asChild size="lg" className="group mt-8 w-full">
              {user ? (
                <Link to="/checkout">
                  Proceed to checkout
                  <ArrowRight className="transition-transform duration-300 group-hover:translate-x-1" />
                </Link>
              ) : (
                <Link to="/login">Please Login</Link>
              )}
            </Button>
            <p className="mt-4 flex items-center justify-center gap-2 text-xs text-muted-foreground">
              <Lock className="h-3 w-3" />
              {user ? 'The final amounts are confirmed when you place the order' : 'Sign in or continue as a guest to check out'}
            </p>
          </div>
        </aside>
      </div>
    </>
  );
};
export default Cart;

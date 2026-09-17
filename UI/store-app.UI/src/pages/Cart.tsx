import { Link } from 'react-router-dom';
import { CartItemsList, Loading, SectionTitle, CartTotals } from '@/components';
import { Button } from '@/components/ui/button';
import { useCart } from '@/features/cart/useCart';
import { useAppSelector } from '@/hooks';

const Cart = () => {
  const user = useAppSelector((state) => state.session.user);
  const { lines, isLoading } = useCart();

  if (isLoading) return <Loading />;
  if (lines.length === 0) return <SectionTitle text="Empty cart" />;

  return (
    <>
      <SectionTitle text="Shopping Cart" />
      <div className="mt-8 grid gap-8 lg:grid-cols-12">
        <div className="lg:col-span-8">
          <CartItemsList lines={lines} />
        </div>
        <div className="lg:col-span-4 lg:pl-4">
          <CartTotals />
          <Button asChild className="mt-8 w-full">
            {user ? <Link to="/checkout">Proceed to checkout</Link> : <Link to="/login">Please Login</Link>}
          </Button>
        </div>
      </div>
    </>
  );
};
export default Cart;

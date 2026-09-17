import { CheckoutForm, Loading, SectionTitle, CartTotals } from '@/components';
import { useCart } from '@/features/cart/useCart';

const Checkout = () => {
  const { lines, isLoading } = useCart();

  if (isLoading) return <Loading />;
  if (lines.length === 0) return <SectionTitle text="Your cart is empty" />;

  return (
    <>
      <SectionTitle text="Place your order" />
      <div className="mt-8 grid gap-8 md:grid-cols-2 items-start">
        <CheckoutForm />
        <CartTotals />
      </div>
    </>
  );
};
export default Checkout;

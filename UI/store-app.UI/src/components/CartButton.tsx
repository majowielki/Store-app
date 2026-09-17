import { Link } from 'react-router-dom';
import { ShoppingCart } from 'lucide-react';
import { useCart } from '@/features/cart/useCart';
import { Button } from './ui/button';

const CartButton = () => {
  const { totalItems } = useCart();
  return (
    <Button asChild variant="outline" size="icon" className="flex justify-center items-center relative">
      <Link to="/cart" aria-label={`Cart, ${totalItems} items`}>
        <ShoppingCart />
        <span className="absolute -top-3 -right-3 bg-primary text-white rounded-full h-6 w-6 flex items-center justify-center text-xs">
          {totalItems}
        </span>
      </Link>
    </Button>
  );
};
export default CartButton;

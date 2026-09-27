import { Link } from 'react-router-dom';
import { Heart } from 'lucide-react';
import { useWishlist } from '@/features/wishlist/useWishlist';
import { CartCount } from './CartButton';
import { Button } from './ui/button';

/** The heart in the header: the wishlist and how many pieces are on it. */
const WishlistLink = () => {
  const { productIds } = useWishlist();
  return (
    <Button asChild variant="ghost" size="icon" className="relative">
      <Link to="/wishlist" aria-label={`Wishlist, ${productIds.length} items`}>
        <Heart className="!h-[1.15rem] !w-[1.15rem]" />
        <CartCount count={productIds.length} />
      </Link>
    </Button>
  );
};

export default WishlistLink;

import { Link, NavLink } from 'react-router-dom';
import { House, LayoutDashboard, LogIn, LogOut, Menu, Package, ShoppingBag, UserPlus, UserRound } from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useCart } from '@/features/cart/useCart';
import { isAdmin } from '@/features/session/roles';
import { useSignOut } from '@/features/session/useSignOut';
import { useAppSelector } from '@/hooks';
import { cn } from '@/lib/utils';
import { CartCount } from './CartButton';

const itemClass = (active = false) =>
  cn(
    'relative flex h-12 flex-1 flex-col items-center justify-center gap-0.5 rounded-full text-[10px] font-medium transition-colors',
    active ? 'text-foreground' : 'text-muted-foreground hover:text-foreground',
  );

/** A floating dock at the bottom of phone screens: menu, home, account and the cart within thumb reach. */
const MobileBottomBar = ({ onMenuClick }: { onMenuClick: () => void }) => {
  const user = useAppSelector((s) => s.session.user);
  const admin = isAdmin(user);
  const { totalItems } = useCart();
  const signOut = useSignOut();

  return (
    <nav
      aria-label="Quick links"
      className="fixed inset-x-4 bottom-4 z-50 mx-auto flex max-w-sm items-center gap-1 rounded-full border bg-background/80 p-1.5 shadow-[0_16px_40px_-12px_rgba(0,0,0,0.35)] backdrop-blur-xl md:hidden"
    >
      <button type="button" onClick={onMenuClick} className={itemClass()} aria-label="Menu">
        <Menu className="h-5 w-5" />
        Menu
      </button>
      <NavLink to="/" end className={({ isActive }) => itemClass(isActive)}>
        <House className="h-5 w-5" />
        Home
      </NavLink>
      <DropdownMenu>
        <DropdownMenuTrigger className={itemClass()} aria-label="My Account">
          <UserRound className="h-5 w-5" />
          Account
        </DropdownMenuTrigger>
        <DropdownMenuContent side="top" align="center" sideOffset={14} className="min-w-[180px]">
          {!user ? (
            <>
              <DropdownMenuItem asChild>
                <Link to="/login">
                  <LogIn />
                  Sign in / Guest
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link to="/register">
                  <UserPlus />
                  Register
                </Link>
              </DropdownMenuItem>
            </>
          ) : (
            <>
              {admin && (
                <DropdownMenuItem asChild>
                  <Link to="/admin">
                    <LayoutDashboard />
                    Dashboard
                  </Link>
                </DropdownMenuItem>
              )}
              <DropdownMenuItem asChild>
                <Link to="/orders">
                  <Package />
                  Orders
                </Link>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => void signOut()}>
                <LogOut />
                Log out
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
      <NavLink
        to="/cart"
        aria-label={`Cart, ${totalItems} items`}
        className={({ isActive }) => cn(itemClass(isActive), isActive && 'bg-foreground text-background hover:text-background')}
      >
        <span className="relative">
          <ShoppingBag className="h-5 w-5" />
          <CartCount count={totalItems} className="-right-2.5 -top-1.5" />
        </span>
        Cart
      </NavLink>
    </nav>
  );
};

export default MobileBottomBar;

import { Menu, ShoppingCart, UserCircle2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Link, useNavigate } from "react-router-dom";
import ModeToggle from "./ModeToggle";
import { useAppSelector, useAppDispatch } from '@/hooks';
import { useState, useRef, useEffect } from 'react';
import { clearCart, clearCartOnServer } from '@/features/cart/cartSlice';
import { logoutUser } from '@/features/user/userSlice';
import { useToast } from '@/hooks/use-toast';


const MobileBottomBar = ({ onMenuClick }: { onMenuClick: () => void }) => {
  const user = useAppSelector((s) => s.userState.user);
  const isAdmin = !!user && (
    user.roles?.some((r) => /admin/i.test(r)) ||
    user.email === 'demoadmin@store.com'
  );
  const numItemsInCart = useAppSelector((state) => state.cartState.numItemsInCart);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { toast } = useToast();
  const [accountOpen, setAccountOpen] = useState(false);
  const accountRef = useRef<HTMLDivElement>(null);

  // Close dropdown on outside click (mobile)
  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (accountOpen && accountRef.current && !accountRef.current.contains(e.target as Node)) {
        setAccountOpen(false);
      }
    }
    if (accountOpen) {
      window.addEventListener('click', handleClick);
    }
    return () => {
      window.removeEventListener('click', handleClick);
    };
  }, [accountOpen]);

  const handleLogout = async () => {
    if (user) {
      await dispatch(clearCartOnServer());
    }
    dispatch(clearCart());
    dispatch(logoutUser());
    toast({ description: 'Logged Out' });
    setAccountOpen(false);
    navigate('/');
  };

  return (
    <nav className="fixed bottom-0 left-0 w-full bg-background border-t z-50 flex justify-around items-center h-16 md:hidden">
      <div className="flex-1 flex justify-around items-center">
        <div className="flex flex-col items-center">
          <Button variant="ghost" size="icon" onClick={onMenuClick} aria-label="Menu">
            <Menu />
          </Button>
          <span className="text-xs mt-0.5">Menu</span>
        </div>
        {/* Account/Login */}
        <div className="flex flex-col items-center relative" ref={accountRef}>
          {!user ? (
            <>
              <Button asChild variant="ghost" size="icon" aria-label="Login">
                <Link to="/login">
                  <UserCircle2 />
                </Link>
              </Button>
              <span className="text-xs mt-0.5">Login</span>
            </>
          ) : (
            <>
              <Button variant="ghost" size="icon" aria-label="My Account" onClick={() => setAccountOpen((v) => !v)}>
                <UserCircle2 />
              </Button>
              <span className="text-xs mt-0.5">My Account</span>
              {accountOpen && (
                <div className="absolute bottom-14 left-1/2 -translate-x-1/2 bg-popover border rounded shadow-lg min-w-[140px] z-50 flex flex-col">
                  {isAdmin && (
                    <Button asChild variant="ghost" className="justify-start px-4 py-2 w-full" onClick={() => { setAccountOpen(false); }}>
                      <Link to="/admin">Dashboard</Link>
                    </Button>
                  )}
                  <Button asChild variant="ghost" className="justify-start px-4 py-2 w-full" onClick={() => { setAccountOpen(false); }}>
                    <Link to="/orders">Orders</Link>
                  </Button>
                  <Button variant="ghost" className="justify-start px-4 py-2 w-full" onClick={handleLogout}>
                    Log out
                  </Button>
                </div>
              )}
            </>
          )}
        </div>
        <div className="flex flex-col items-center">
          <Button asChild variant="ghost" size="icon" aria-label="Cart" className="relative">
            <Link to="/cart">
              <ShoppingCart />
              {numItemsInCart > 0 && (
                <span className="absolute -top-2 -right-2 bg-primary text-white rounded-full h-5 w-5 flex items-center justify-center text-xs">
                  {numItemsInCart}
                </span>
              )}
            </Link>
          </Button>
          <span className="text-xs mt-0.5">Cart</span>
        </div>
        <div className="flex flex-col items-center">
          <ModeToggle />
          <span className="text-xs mt-0.5">Theme</span>
        </div>
      </div>
    </nav>
  );
};

export default MobileBottomBar;

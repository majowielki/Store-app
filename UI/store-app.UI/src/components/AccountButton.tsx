import { Link, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { UserCircle2 } from 'lucide-react';
// import ModeToggle from './ModeToggle';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useEffect, useRef, useState } from 'react';
import { useToast } from '@/hooks/use-toast';
import { clearCart, clearCartOnServer } from '@/features/cart/cartSlice';
import { logoutUser } from '@/features/user/userSlice';

const AccountButton = () => {
  const user = useAppSelector((s) => s.userState.user);
  const isAdmin = !!user && (
    user.roles?.some((r) => /admin/i.test(r)) ||
    user.email === 'demoadmin@store.com'
  );
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { toast } = useToast();
  const [open, setOpen] = useState(false);
  const closeTimeout = useRef<number | null>(null);
  const triggerRef = useRef<HTMLButtonElement | null>(null);
  const contentRef = useRef<HTMLDivElement | null>(null);

  const clearCloseTimeout = () => {
    if (closeTimeout.current) {
      window.clearTimeout(closeTimeout.current);
      closeTimeout.current = null;
    }
  };

  useEffect(() => {
    if (!open) return;
    const padding = 6; // small grace area
    const handleMouseMove = (e: MouseEvent) => {
      const x = e.clientX;
      const y = e.clientY;
      const isInside = (el: HTMLElement | null): boolean => {
        if (!el) return false;
        const r = el.getBoundingClientRect();
        return (
          x >= r.left - padding &&
          x <= r.right + padding &&
          y >= r.top - padding &&
          y <= r.bottom + padding
        );
      };
      const overTrigger = isInside(triggerRef.current);
      const overContent = isInside(contentRef.current);
      if (overTrigger || overContent) {
        clearCloseTimeout();
      } else {
        clearCloseTimeout();
        closeTimeout.current = window.setTimeout(() => setOpen(false), 120);
      }
    };
    document.addEventListener('mousemove', handleMouseMove);
    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      clearCloseTimeout();
    };
  }, [open]);

  const handleLogout = async () => {
    if (user) {
      await dispatch(clearCartOnServer());
    }
    dispatch(clearCart());
    dispatch(logoutUser());
    toast({ description: 'Logged Out' });
    navigate('/');
    setOpen(false);
  };

  return (
    <div className="flex items-center gap-2 sm:gap-3">
  <DropdownMenu open={open} onOpenChange={setOpen}>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            className="gap-2"
            aria-haspopup="menu"
    ref={triggerRef}
    onMouseEnter={() => setOpen(true)}
    onClick={() => setOpen((v) => !v)}
          >
            <UserCircle2 className="h-5 w-5" />
            <span className="hidden sm:inline">My Account</span>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="end"
          className="min-w-[180px]"
      ref={contentRef}
      onInteractOutside={() => setOpen(false)}
        >
          {!user ? (
            <>
              <DropdownMenuItem asChild>
                <Link to="/login" onClick={() => setOpen(false)}>
                  Sign in / Guest
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link to="/register" onClick={() => setOpen(false)}>
                  Register
                </Link>
              </DropdownMenuItem>
            </>
          ) : (
            <>
              {isAdmin && (
                <DropdownMenuItem asChild>
                  <Link to="/admin" onClick={() => setOpen(false)}>
                    Dashboard
                  </Link>
                </DropdownMenuItem>
              )}
              <DropdownMenuItem asChild>
                <Link to="/orders" onClick={() => setOpen(false)}>
                  Orders
                </Link>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={handleLogout}>Log out</DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>

  {/* ModeToggle removed to prevent duplicate theme switch in header */}
    </div>
  );
};

export default AccountButton;

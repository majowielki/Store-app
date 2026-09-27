import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { LayoutDashboard, LogIn, LogOut, Package, UserPlus, UserRound } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { isAdmin } from '@/features/session/roles';
import { useSignOut } from '@/features/session/useSignOut';
import { useAppSelector } from '@/hooks';

const AccountButton = () => {
  const user = useAppSelector((s) => s.session.user);
  const admin = isAdmin(user);
  const signOut = useSignOut();
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

  // The menu opens on hover and closes once the pointer has left both the trigger and the menu
  useEffect(() => {
    if (!open) return;
    const padding = 6;
    const handleMouseMove = (e: MouseEvent) => {
      const isInside = (el: HTMLElement | null): boolean => {
        if (!el) return false;
        const r = el.getBoundingClientRect();
        return e.clientX >= r.left - padding && e.clientX <= r.right + padding && e.clientY >= r.top - padding && e.clientY <= r.bottom + padding;
      };
      clearCloseTimeout();
      if (!isInside(triggerRef.current) && !isInside(contentRef.current)) {
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
    setOpen(false);
    await signOut();
  };

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          aria-haspopup="menu"
          ref={triggerRef}
          onMouseEnter={() => setOpen(true)}
          onClick={() => setOpen((v) => !v)}
        >
          <UserRound className="h-[1.15rem]! w-[1.15rem]!" />
          <span className="sr-only">My Account</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="min-w-[220px]" ref={contentRef} onInteractOutside={() => setOpen(false)}>
        {!user ? (
          <>
            <DropdownMenuLabel className="font-normal">
              <p className="display text-lg">Welcome</p>
              <p className="text-xs text-muted-foreground">Sign in to see your orders</p>
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuItem asChild>
              <Link to="/login" onClick={() => setOpen(false)}>
                <LogIn />
                Sign in / Guest
              </Link>
            </DropdownMenuItem>
            <DropdownMenuItem asChild>
              <Link to="/register" onClick={() => setOpen(false)}>
                <UserPlus />
                Register
              </Link>
            </DropdownMenuItem>
          </>
        ) : (
          <>
            <DropdownMenuLabel className="font-normal">
              <p className="display text-lg">Hi, {user.firstName || user.userName}</p>
              <p className="truncate text-xs text-muted-foreground">{user.email}</p>
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
            {admin && (
              <DropdownMenuItem asChild>
                <Link to="/admin" onClick={() => setOpen(false)}>
                  <LayoutDashboard />
                  Dashboard
                </Link>
              </DropdownMenuItem>
            )}
            <DropdownMenuItem asChild>
              <Link to="/orders" onClick={() => setOpen(false)}>
                <Package />
                Orders
              </Link>
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={handleLogout}>
              <LogOut />
              Log out
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
};

export default AccountButton;

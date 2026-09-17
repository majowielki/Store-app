import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { UserCircle2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
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
        <DropdownMenuContent align="end" className="min-w-[180px]" ref={contentRef} onInteractOutside={() => setOpen(false)}>
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
              {admin && (
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
    </div>
  );
};

export default AccountButton;

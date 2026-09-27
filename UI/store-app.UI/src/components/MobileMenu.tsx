import { Link } from 'react-router-dom';
import { ArrowUpRight, Plus } from 'lucide-react';
import { useGetProductsMetaQuery } from '@/api/catalog';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/components/ui/sheet';
import { isAdmin } from '@/features/session/roles';
import { useSignOut } from '@/features/session/useSignOut';
import { useAppSelector } from '@/hooks';
import { inspirationPages } from '@/content/pages';
import { categories, categoryHref } from '@/utils/categories';
import Logo from './Logo';
import { ThemeSwitch } from './ModeToggle';

interface MobileMenuProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/** The whole shop in a drawer from the left, for screens too narrow for the header's sections. */
const MobileMenu = ({ open, onOpenChange }: MobileMenuProps) => {
  const user = useAppSelector((s) => s.session.user);
  const admin = isAdmin(user);
  const signOut = useSignOut();
  const { data: meta } = useGetProductsMetaQuery();
  const close = () => onOpenChange(false);
  const rooms = categories.filter((c) => c.group !== 'sale');

  const secondary = [
    ...(admin ? [{ to: '/admin', label: 'Dashboard' }] : []),
    ...(user ? [{ to: '/orders', label: 'My orders' }] : []),
    { to: '/about', label: 'About us' },
    { to: '/contact', label: 'Contact' },
  ];

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="left" className="flex w-[88vw] max-w-sm flex-col gap-0 p-0">
        <SheetTitle className="sr-only">Menu</SheetTitle>
        <SheetDescription className="sr-only">The shop's rooms and pages</SheetDescription>
        <div className="flex h-16 items-center border-b px-6">
          <Logo onClick={close} />
        </div>

        <nav aria-label="Shop" className="flex-1 overflow-y-auto px-6 py-6">
          <ul className="divide-y">
            <li className="animate-fade-up">
              <Link to="/products" onClick={close} className="display flex items-center justify-between py-3 text-2xl">
                Shop all
                <ArrowUpRight className="h-5 w-5 text-muted-foreground" />
              </Link>
            </li>
            {rooms.map((room, index) => {
              const entry = meta?.groupCategoryMap.find((g) => g.key === room.group);
              return (
                <li key={room.slug} className="animate-fade-up" style={{ animationDelay: `${(index + 1) * 45}ms` }}>
                  <details className="group">
                    <summary className="display flex cursor-pointer list-none items-center justify-between py-3 text-2xl [&::-webkit-details-marker]:hidden">
                      {room.label}
                      <Plus className="h-5 w-5 text-muted-foreground transition-transform duration-300 group-open:rotate-45" />
                    </summary>
                    <ul className="grid gap-2.5 pb-4 pl-0.5">
                      <li>
                        <Link to={categoryHref(room)} onClick={close} className="text-sm font-medium">
                          Everything in {room.label.toLowerCase()}
                        </Link>
                      </li>
                      {entry?.categories.map((c) => (
                        <li key={c.key}>
                          <Link
                            to={`/products?group=${encodeURIComponent(room.group)}&category=${encodeURIComponent(c.key)}`}
                            onClick={close}
                            className="text-sm text-muted-foreground transition-colors hover:text-foreground"
                          >
                            {c.name}
                          </Link>
                        </li>
                      ))}
                    </ul>
                  </details>
                </li>
              );
            })}
            <li className="animate-fade-up" style={{ animationDelay: `${(rooms.length + 1) * 45}ms` }}>
              <Link to="/products?sale=on" onClick={close} className="display flex items-center justify-between py-3 text-2xl text-brand">
                Sale
                <ArrowUpRight className="h-5 w-5" />
              </Link>
            </li>
          </ul>

          <p className="eyebrow mt-8">Inspiration</p>
          <ul className="mt-3 grid grid-cols-2 gap-3">
            {inspirationPages.map((item) => (
              <li key={item.to}>
                <Link to={item.to} onClick={close} className="text-sm font-medium">
                  {item.label}
                </Link>
              </li>
            ))}
          </ul>

          <ul className="mt-8 grid gap-3">
            {secondary.map((item) => (
              <li key={item.to}>
                <Link to={item.to} onClick={close} className="text-sm text-muted-foreground transition-colors hover:text-foreground">
                  {item.label}
                </Link>
              </li>
            ))}
          </ul>
        </nav>

        <div className="flex items-center justify-between gap-3 border-t px-6 py-4">
          {user ? (
            <Button
              variant="outline"
              size="sm"
              onClick={async () => {
                close();
                await signOut();
              }}
            >
              Log out
            </Button>
          ) : (
            <div className="flex gap-2">
              <Button asChild size="sm">
                <Link to="/login" onClick={close}>
                  Sign in
                </Link>
              </Button>
              <Button asChild size="sm" variant="outline">
                <Link to="/register" onClick={close}>
                  Register
                </Link>
              </Button>
            </div>
          )}
          <ThemeSwitch />
        </div>
      </SheetContent>
    </Sheet>
  );
};

export default MobileMenu;

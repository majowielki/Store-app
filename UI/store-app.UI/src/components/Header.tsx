import { useEffect, useState } from 'react';
import { Menu, Search } from 'lucide-react';
import { useScrollState } from '@/hooks/use-scroll-state';
import { useIsMobile } from '@/hooks/use-mobile';
import { cn } from '@/lib/utils';
import AccountButton from './AccountButton';
import CartButton from './CartButton';
import WishlistLink from './WishlistLink';
import DesktopNav from './DesktopNav';
import Logo from './Logo';
import MobileBottomBar from './MobileBottomBar';
import MobileMenu from './MobileMenu';
import ModeToggle from './ModeToggle';
import SearchDialog from './SearchDialog';
import { Button } from './ui/button';

const isMac = typeof navigator !== 'undefined' && /Mac|iPhone|iPad/.test(navigator.userAgent);

/**
 * The shop's header: sticky, blurred once the page scrolls, out of the way while scrolling
 * down and back on the way up. Wide screens get the sections inline (with their panels);
 * narrower ones a drawer, and phones a dock at the bottom instead of the account and cart buttons.
 */
const Header = () => {
  const isMobile = useIsMobile();
  const { scrolled, hidden } = useScrollState();
  const [menuOpen, setMenuOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);

  // Ctrl+K (Cmd+K) opens the search from anywhere
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault();
        setSearchOpen(true);
      }
    };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, []);

  return (
    <>
      <header
        className={cn(
          'sticky top-0 z-40 transition-[transform,background-color,border-color] duration-500 ease-smooth',
          scrolled ? 'border-b bg-background/80 backdrop-blur-xl' : 'border-b border-transparent bg-background',
          hidden && !menuOpen && !searchOpen ? '-translate-y-full' : 'translate-y-0',
        )}
      >
        <div className="align-element flex h-16 items-center gap-2 md:h-18 xl:grid xl:grid-cols-[1fr_auto_1fr]">
          <div className="flex items-center gap-1">
            <Button variant="ghost" size="icon" className="-ml-2 hidden md:inline-flex xl:hidden" onClick={() => setMenuOpen(true)} aria-label="Menu">
              <Menu className="h-5! w-5!" />
            </Button>
            <Logo />
          </div>

          <DesktopNav className="hidden xl:block" />

          <div className="ml-auto flex items-center justify-end gap-1">
            <button
              type="button"
              onClick={() => setSearchOpen(true)}
              className="hidden h-10 w-48 items-center gap-2 rounded-full border px-4 text-sm text-muted-foreground transition-colors hover:border-foreground/40 hover:text-foreground md:flex lg:w-56"
            >
              <Search className="h-4 w-4" />
              <span className="flex-1 text-left">Search</span>
              <kbd className="rounded border bg-muted px-1.5 font-sans text-[10px] font-medium">{isMac ? '⌘K' : 'Ctrl K'}</kbd>
            </button>
            <Button variant="ghost" size="icon" className="md:hidden" onClick={() => setSearchOpen(true)} aria-label="Search">
              <Search className="h-5! w-5!" />
            </Button>
            <WishlistLink />
            {!isMobile && (
              <>
                <AccountButton />
                <ModeToggle />
                <CartButton />
              </>
            )}
          </div>
        </div>
      </header>
      {isMobile && <MobileBottomBar onMenuClick={() => setMenuOpen(true)} />}
      <MobileMenu open={menuOpen} onOpenChange={setMenuOpen} />
      <SearchDialog open={searchOpen} onOpenChange={setSearchOpen} />
    </>
  );
};

export default Header;

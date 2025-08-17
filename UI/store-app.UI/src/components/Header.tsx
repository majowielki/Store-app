

import Logo from './Logo';
import SearchBar from './SearchBar';
import AccountButton from './AccountButton';
import MobileBottomBar from './MobileBottomBar';
import CartButton from './CartButton';
import ModeToggle from './ModeToggle';
import { useIsMobile } from '@/hooks/use-mobile';
import { useState } from 'react';
import CustomSidebar from './CustomSidebar';
import { useLocation } from 'react-router-dom';


const Header = () => {
  const isMobile = useIsMobile();
  const [menuOpen, setMenuOpen] = useState(false);
  const location = useLocation();
  const isAdminRoute = location.pathname.startsWith('/admin');

  // On mobile/tablet and not on admin dashboard, hide account, cart, and theme switch (they are in bottom bar)
  const showUtils = !isMobile || isAdminRoute;

  return (
    <>
      <header className="bg-background">
        <div className="align-element flex items-center justify-between gap-4 py-3">
          <div className="flex items-center gap-4 min-w-[120px]">
            <Logo />
          </div>
          <div className="flex-1 flex justify-center">
            <SearchBar />
          </div>
          {showUtils && (
            <div className="flex items-center gap-2 min-w-[80px] justify-end">
              <AccountButton />
              <CartButton />
              {/* Only show ModeToggle on mobile/tablet or on admin route to avoid duplicate on desktop */}
              {(!isMobile || isAdminRoute) && <ModeToggle />}
            </div>
          )}
        </div>
      </header>
      {isMobile && !isAdminRoute && <MobileBottomBar onMenuClick={() => setMenuOpen(true)} />}
      <CustomSidebar open={menuOpen} setOpen={setMenuOpen} />
    </>
  );
};

export default Header;

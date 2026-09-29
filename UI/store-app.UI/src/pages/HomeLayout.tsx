import { useRef } from 'react';
import { Outlet, ScrollRestoration, useLocation, useNavigation } from 'react-router-dom';
import { Header, Loading } from '@/components';
import AnnouncementBar from '@/components/AnnouncementBar';
import CartDrawer from '@/components/CartDrawer';
import CompareBar from '@/components/CompareBar';
import Footer from '@/components/Footer';
import SkipLink from '@/components/SkipLink';
import { useFocusOnNavigate } from '@/hooks/use-focus-on-navigate';
import { MAIN_CONTENT_ID } from '@/lib/focus';
import { cn } from '@/lib/utils';

const HomeLayout = () => {
  const navigation = useNavigation();
  const isPageLoading = navigation.state === 'loading';
  const { pathname } = useLocation();
  // The landing page lays out its own full-width sections; every other page sits in the column
  const fullBleed = pathname === '/';
  const main = useRef<HTMLElement>(null);
  useFocusOnNavigate(main);

  return (
    <div className="flex min-h-screen flex-col">
      <SkipLink />
      <AnnouncementBar />
      <Header />
      <main id={MAIN_CONTENT_ID} ref={main} tabIndex={-1} className={cn('flex-1 focus:outline-none', !fullBleed && 'align-element py-8 md:py-12')}>
        {isPageLoading ? <Loading /> : <Outlet />}
      </main>
      <Footer />
      <CartDrawer />
      <CompareBar />
      {/* A new page starts at the top; Back returns to where the visitor was */}
      <ScrollRestoration />
    </div>
  );
};
export default HomeLayout;

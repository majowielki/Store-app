import { Outlet, ScrollRestoration, useLocation, useNavigation } from 'react-router-dom';
import { Header, Loading } from '@/components';
import AnnouncementBar from '@/components/AnnouncementBar';
import Footer from '@/components/Footer';
import { cn } from '@/lib/utils';

const HomeLayout = () => {
  const navigation = useNavigation();
  const isPageLoading = navigation.state === 'loading';
  const { pathname } = useLocation();
  // The landing page lays out its own full-width sections; every other page sits in the column
  const fullBleed = pathname === '/';

  return (
    <div className="flex min-h-screen flex-col">
      <AnnouncementBar />
      <Header />
      <main className={cn('flex-1', !fullBleed && 'align-element py-8 md:py-12')}>{isPageLoading ? <Loading /> : <Outlet />}</main>
      <Footer />
      {/* A new page starts at the top; Back returns to where the visitor was */}
      <ScrollRestoration />
    </div>
  );
};
export default HomeLayout;

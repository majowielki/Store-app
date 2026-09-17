import { useEffect } from 'react';
import { Outlet, useLocation, useNavigation } from 'react-router-dom';
import { Header, Loading, Navbar } from '@/components';
import Footer from '@/components/Footer';

const HomeLayout = () => {
  const navigation = useNavigation();
  const isPageLoading = navigation.state === 'loading';

  // Scroll to top on route change
  const { pathname, search } = useLocation();
  useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: 'smooth' });
  }, [pathname, search]);

  return (
    <>
      <Header />
      <Navbar />
      <div className="align-element py-20">{isPageLoading ? <Loading /> : <Outlet />}</div>
      <Footer />
    </>
  );
};
export default HomeLayout;

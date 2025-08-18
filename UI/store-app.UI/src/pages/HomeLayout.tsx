import { Outlet, useNavigation, useLocation } from "react-router-dom";
import React, { useEffect } from 'react';
import { Header, Loading, Navbar } from "@/components";
// ...existing code...
import Footer from '@/components/Footer';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { fetchCart } from '@/features/cart/cartSlice';
import { getCurrentUserAsync } from '@/features/user/userSlice';

const HomeLayout = () => {
  const useAuthMe = import.meta.env.VITE_USE_AUTH_ME === 'true';
  const navigation = useNavigation();
  const isPageLoading = navigation.state === "loading";
  const dispatch = useAppDispatch();
  const token = useAppSelector((s) => s.userState.token);
  const user = useAppSelector((s) => s.userState.user);
  const userLoading = useAppSelector((s) => s.userState.isLoading);
  const meAttempted = useAppSelector((s) => s.userState.meAttempted);

  // Scroll to top on route change
  const { pathname, search } = useLocation();
  React.useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: 'smooth' });
  }, [pathname, search]);

  // If we have a token but no user yet, validate token and load user first
  useEffect(() => {
    if (useAuthMe && token && !user && !userLoading && !meAttempted) {
      dispatch(getCurrentUserAsync());
    }
  }, [useAuthMe, token, user, userLoading, meAttempted, dispatch]);

  // Fetch cart only after user is known (prevents 401 from stale/invalid tokens on startup)
  useEffect(() => {
    if (token && user) {
      dispatch(fetchCart());
    }
  }, [token, user, dispatch]);

  return (
    <>
      <Header />
      <Navbar />
  {/* ...existing code... */}
      <div className="align-element py-20">
        {isPageLoading ? <Loading /> : <Outlet />}
      </div>
      <Footer />
    </>
  );
};
export default HomeLayout;

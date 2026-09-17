import { Outlet, useNavigation, useLocation } from "react-router-dom";
import React, { useEffect } from 'react';
import { Header, Loading, Navbar } from "@/components";
import Footer from '@/components/Footer';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { fetchCart } from '@/features/cart/cartSlice';
import { useRef } from 'react';

const HomeLayout = () => {
  const navigation = useNavigation();
  const isPageLoading = navigation.state === "loading";
  const dispatch = useAppDispatch();
  const user = useAppSelector((s) => s.userState.user);
  const sessionChecked = useAppSelector((s) => s.userState.sessionChecked);

  // Scroll to top on route change
  const { pathname, search } = useLocation();
  React.useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: 'smooth' });
  }, [pathname, search]);

  // On first app load, always initialize Redux cart state from localStorage
  const initialized = useRef(false);
  useEffect(() => {
    if (!initialized.current) {
      const cart = localStorage.getItem('cart');
      if (cart) {
        try {
          const parsed = JSON.parse(cart);
          dispatch({ type: 'cart/syncWithServer', payload: parsed });
        } catch {
          // ignore JSON parse errors
        }
      }
      initialized.current = true;
    }
  }, [dispatch]);

  // Fetch the server cart once the session is confirmed (a cached profile alone is not a session)
  useEffect(() => {
    // Only fetch cart if we did NOT just merge a guest cart (after login/registration)
    if (sessionChecked && user) {
      const justMerged = sessionStorage.getItem('justMergedGuestCart');
      if (!justMerged) {
        dispatch(fetchCart());
      } else {
        sessionStorage.removeItem('justMergedGuestCart');
      }
    }
  }, [sessionChecked, user, dispatch]);

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

import { Outlet, useNavigation } from "react-router-dom";
import { Header, Loading, Navbar } from "@/components";
import { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { fetchCart } from '@/features/cart/cartSlice';
import { getCurrentUserAsync } from '@/features/user/userSlice';

const HomeLayout = () => {
  const navigation = useNavigation();
  const isPageLoading = navigation.state === "loading";
  const dispatch = useAppDispatch();
  const token = useAppSelector((s) => s.userState.token);
  const user = useAppSelector((s) => s.userState.user);
  const userLoading = useAppSelector((s) => s.userState.isLoading);
  const meAttempted = useAppSelector((s) => s.userState.meAttempted);

  // If we have a token but no user yet, validate token and load user first
  useEffect(() => {
    if (token && !user && !userLoading && !meAttempted) {
      dispatch(getCurrentUserAsync());
    }
  }, [token, user, userLoading, meAttempted, dispatch]);

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
      <div className="align-element py-20">
        {isPageLoading ? <Loading /> : <Outlet />}
      </div>
    </>
  );
};
export default HomeLayout;

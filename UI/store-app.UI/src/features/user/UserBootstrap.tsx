import { useEffect } from 'react';
import { useAppDispatch } from '@/hooks';
import { getCurrentUserAsync } from '@/features/user/userSlice';

export function UserBootstrap() {
  const dispatch = useAppDispatch();
  useEffect(() => {
    const token = localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
    if (token) {
      dispatch(getCurrentUserAsync());
    }
  }, [dispatch]);
  return null;
}

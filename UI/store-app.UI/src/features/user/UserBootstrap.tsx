import { useEffect } from 'react';
import { useAppDispatch } from '@/hooks';
import { restoreSessionAsync } from '@/features/user/userSlice';

/** Once per page load: continue the session from the refresh cookie, or settle as anonymous. */
export function UserBootstrap() {
  const dispatch = useAppDispatch();
  useEffect(() => {
    dispatch(restoreSessionAsync());
  }, [dispatch]);
  return null;
}

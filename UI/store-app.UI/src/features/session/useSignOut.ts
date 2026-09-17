import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch } from '@/hooks';
import { toast } from '@/hooks/use-toast';
import { signOut } from './sessionThunks';

/** Signs out and returns to the home page; used by both account menus. */
export const useSignOut = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  return useCallback(async () => {
    await dispatch(signOut());
    toast({ description: 'Logged out' });
    navigate('/');
  }, [dispatch, navigate]);
};

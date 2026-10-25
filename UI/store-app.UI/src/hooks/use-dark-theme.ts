import { useEffect, useState } from 'react';
import { DARK_SCHEME_QUERY } from '@/utils/applyTheme';
import { useAppSelector } from './redux';

/** Whether the shop is shown dark now: the dark theme chosen, or "system" on a system set to dark (followed live). */
export const useDarkTheme = () => {
  const theme = useAppSelector((state) => state.theme.theme);
  const [systemDark, setSystemDark] = useState(() => typeof window.matchMedia === 'function' && window.matchMedia(DARK_SCHEME_QUERY).matches);

  useEffect(() => {
    if (typeof window.matchMedia !== 'function') return;
    const query = window.matchMedia(DARK_SCHEME_QUERY);
    const onChange = (event: MediaQueryListEvent) => setSystemDark(event.matches);
    query.addEventListener?.('change', onChange);
    return () => query.removeEventListener?.('change', onChange);
  }, []);

  return theme === 'dark' || (theme === 'system' && systemDark);
};

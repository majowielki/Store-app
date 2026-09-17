import { redirect } from 'react-router-dom';
import { isAdmin } from '@/features/session/roles';
import { toast } from '@/hooks/use-toast';
import { store } from '@/store';

/** Resolves once the session was restored or refused after the page load (see main.tsx). */
const whenSessionChecked = (): Promise<void> =>
  new Promise((resolve) => {
    if (store.getState().session.checked) return resolve();
    const unsubscribe = store.subscribe(() => {
      if (store.getState().session.checked) {
        unsubscribe();
        resolve();
      }
    });
  });

// Route loaders that only decide who may enter. They wait for the session check, so a hard
// reload of /admin or /checkout is judged by the profile /auth/me returned - never by what a
// previous visit left in the browser.

export const requireUser = async () => {
  await whenSessionChecked();
  if (!store.getState().session.user) {
    toast({ description: 'Please sign in to continue' });
    return redirect('/login');
  }
  return null;
};

export const requireAdmin = async () => {
  await whenSessionChecked();
  return isAdmin(store.getState().session.user) ? null : redirect('/');
};

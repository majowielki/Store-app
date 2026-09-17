// The access token lives here, in memory, for the lifetime of the page: a script injected
// into the page cannot read it from storage, and a reload gets a new one from the refresh
// token, which the browser keeps in an httpOnly cookie the page never sees.

let accessToken: string | null = null;
const endedListeners = new Set<() => void>();

/**
 * Whether a session was started in this browser and not ended since. The page cannot see the
 * refresh cookie, so this is what tells a page load whether asking for a new access token is
 * worth a request; a visitor who never signed in is not sent to /auth/refresh to be refused.
 */
const REMEMBERED_KEY = 'store.session';

export const getAccessToken = (): string | null => accessToken;

export const setAccessToken = (token: string | null): void => {
  accessToken = token;
};

export const hasRememberedSession = (): boolean => {
  try {
    return localStorage.getItem(REMEMBERED_KEY) === '1';
  } catch {
    return false;
  }
};

export const rememberSession = (started: boolean): void => {
  try {
    if (started) localStorage.setItem(REMEMBERED_KEY, '1');
    else localStorage.removeItem(REMEMBERED_KEY);
  } catch {
    // Without storage every page load asks the server, which still works
  }
};

/** Called when the session cannot be continued (refresh refused): the app forgets the user. */
export const onSessionEnded = (listener: () => void): (() => void) => {
  endedListeners.add(listener);
  return () => endedListeners.delete(listener);
};

export const endSession = (): void => {
  accessToken = null;
  rememberSession(false);
  for (const listener of endedListeners) listener();
};

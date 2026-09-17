// The access token lives here, in memory, for the lifetime of the page: a script injected
// into the page cannot read it from storage, and a reload gets a new one from the refresh
// token, which the browser keeps in an httpOnly cookie the page never sees.

let accessToken: string | null = null;
const endedListeners = new Set<() => void>();

export const getAccessToken = (): string | null => accessToken;

export const setAccessToken = (token: string | null): void => {
  accessToken = token;
};

/** Called when the session cannot be continued (refresh refused): the app forgets the user. */
export const onSessionEnded = (listener: () => void): (() => void) => {
  endedListeners.add(listener);
  return () => endedListeners.delete(listener);
};

export const endSession = (): void => {
  accessToken = null;
  for (const listener of endedListeners) listener();
};

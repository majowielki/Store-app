import { render, type RenderOptions } from '@testing-library/react';
import type { ReactElement, ReactNode } from 'react';
import { Provider } from 'react-redux';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { setAccessToken } from '@/api/session';
import type { UserResponse } from '@/api/types';
import { sessionStarted } from '@/features/session/sessionSlice';
import { Toaster } from '@/components/ui/toaster';
import { createAppStore, type RootState } from '@/store';

interface Options extends Omit<RenderOptions, 'wrapper'> {
  /** Start signed in as this user, with an access token in place. */
  user?: UserResponse;
  route?: string;
  /** The route pattern the element is mounted at, when the page reads params. */
  path?: string;
  preloadedState?: Partial<RootState>;
}

/** A fresh store and router per test, the way main.tsx wires them for the app. */
export const renderWithStore = (ui: ReactElement, { user, route = '/', path = '*', preloadedState, ...options }: Options = {}) => {
  const store = createAppStore(preloadedState);
  if (user) {
    setAccessToken(`token-for-${user.id}`);
    store.dispatch(sessionStarted(user));
  }
  const Wrapper = ({ children }: { children: ReactNode }) => (
    <Provider store={store}>
      <Toaster />
      <MemoryRouter initialEntries={[route]}>
        <Routes>
          <Route path={path} element={children} />
        </Routes>
      </MemoryRouter>
    </Provider>
  );
  return { store, ...render(ui, { wrapper: Wrapper, ...options }) };
};

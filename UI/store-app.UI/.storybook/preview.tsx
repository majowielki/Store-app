import '@fontsource-variable/fraunces/opsz.css';
import '@fontsource-variable/fraunces/opsz-italic.css';
import '@fontsource-variable/geist';
import '../src/index.css';
import './preview.css';
import { useEffect, useState, type ReactNode } from 'react';
import type { Decorator, Preview } from '@storybook/react-vite';
import { mswLoader } from 'msw-storybook-addon/csf3';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import { Toaster } from '@/components/ui/toaster';
import { applyTheme } from '@/utils/applyTheme';
import type { Theme } from '@/features/theme/themeSlice';
import { createAppStore, type RootState } from '@/store';
import { storyHandlers } from '@/stories/data';
import { handlers } from '@/test/handlers';

/**
 * A fresh store per story, so no story sees what another one put in the cart. A story's
 * parameters.store is the state it starts from (a guest cart, a signed-in user...).
 */
const StoryStore = ({ state, children }: { state?: Partial<RootState>; children: ReactNode }) => {
  const [store] = useState(() => createAppStore(state));
  return (
    <Provider store={store}>
      <Toaster />
      {children}
    </Provider>
  );
};

const withStore: Decorator = (Story, { parameters }) => (
  <StoryStore state={parameters.store}>
    <Story />
  </StoryStore>
);

/** A story's parameters.route is the address its router is at, for components that read the URL. */
const withRouter: Decorator = (Story, { parameters }) => (
  <MemoryRouter initialEntries={[parameters.route ?? '/']}>
    <Story />
  </MemoryRouter>
);

/** The theme from the toolbar, applied the way the app applies it: a class on <html>. */
const StoryTheme = ({ theme, children }: { theme: Theme; children: ReactNode }) => {
  useEffect(() => applyTheme(theme), [theme]);
  return children;
};

const withTheme: Decorator = (Story, { globals }) => (
  <StoryTheme theme={globals.theme === 'dark' ? 'dark' : 'light'}>
    <Story />
  </StoryTheme>
);

const preview: Preview = {
  decorators: [withTheme, withRouter, withStore],
  loaders: [mswLoader()],
  parameters: {
    /**
     * The stories render the shop's own components against the API the unit tests use: the MSW
     * handlers of src/test answer every request in the browser, with the same fixtures. The first
     * handler that matches answers, so a story's own go in "overrides", which comes first.
     */
    msw: { handlers: { overrides: [], stories: storyHandlers, api: handlers } },
    layout: 'padded',
    backgrounds: { disable: true },
    controls: { matchers: { color: /(background|color)$/i } },
  },
  globalTypes: {
    theme: {
      description: 'Light or dark theme',
      toolbar: { title: 'Theme', icon: 'mirror', items: ['light', 'dark'], dynamicTitle: true },
    },
  },
  initialGlobals: { theme: 'light' },
};

export default preview;

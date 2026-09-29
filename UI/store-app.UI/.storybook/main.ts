import type { StorybookConfig } from '@storybook/react-vite';

/** Plugins of vite.config.ts that only concern the app's own index.html (the font preloads). */
const appPagePlugins = new Set(['store:preload-fonts']);

const config: StorybookConfig = {
  stories: ['../src/**/*.stories.tsx'],
  addons: ['@storybook/addon-a11y'],
  framework: '@storybook/react-vite',
  staticDirs: [
    // The mock service worker (msw init): the stories' API calls are answered in the browser
    './public',
    // Real product pictures, the 400 px copies (ADR 016), at /pictures/<Name>.webp
    { from: '../../../Blobs/w400', to: '/pictures' },
  ],
  core: { disableTelemetry: true },
  viteFinal: (vite) => ({
    ...vite,
    plugins: vite.plugins?.flat().filter((plugin) => !(plugin && 'name' in plugin && appPagePlugins.has(plugin.name))),
  }),
};

export default config;

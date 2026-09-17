import { defineConfig, mergeConfig } from 'vitest/config';
import viteConfig from './vite.config';

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      environment: 'jsdom',
      env: { VITE_API_BASE_URL: 'http://localhost/api/v1' },
      globals: false,
      setupFiles: ['./src/test/setup.ts'],
      include: ['src/**/*.test.{ts,tsx}'],
      css: false,
      restoreMocks: true,
      clearMocks: true,
      coverage: {
        provider: 'v8',
        reporter: ['text', 'cobertura'],
        include: ['src/**/*.{ts,tsx}'],
        exclude: ['src/api/schema/**', 'src/components/ui/**', 'src/test/**', 'src/**/*.test.{ts,tsx}', 'src/main.tsx'],
      },
    },
  }),
);

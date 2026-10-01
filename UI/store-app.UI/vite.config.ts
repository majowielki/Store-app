import { defineConfig, type Plugin } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "path";

/**
 * The font files the first screen draws with: the Latin subsets of Fraunces (the headings, upright
 * and italic) and of Geist (the text). src/main.tsx imports their style sheets.
 */
const FIRST_SCREEN_FONTS = ["fraunces-latin-opsz-normal", "fraunces-latin-opsz-italic", "geist-latin-wght-normal"];

/**
 * Asks for the fonts of the first screen in index.html itself (link rel=preload), so they load
 * alongside the script instead of after the style sheet has been read. A font the build no longer
 * emits fails the build, rather than preloading nothing without a word.
 */
const preloadFonts = (names: string[]): Plugin => ({
  name: "store:preload-fonts",
  apply: "build",
  transformIndexHtml: {
    order: "post",
    handler: (_html, { bundle }) =>
      names.map((name) => {
        const file = Object.keys(bundle ?? {}).find((fileName) => fileName.startsWith(`assets/${name}-`) && fileName.endsWith(".woff2"));
        if (!file) throw new Error(`The font ${name} is not in the build; update FIRST_SCREEN_FONTS in vite.config.ts`);
        return { tag: "link", attrs: { rel: "preload", as: "font", type: "font/woff2", href: `/${file}`, crossorigin: "" }, injectTo: "head" as const };
      }),
  },
});

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss(), preloadFonts(FIRST_SCREEN_FONTS)],
  build: {
    // Pictures stay files, the swatch textures too: a picture inlined into a script is parsed with
    // it on every page, while a file is fetched when a swatch shows it and then cached
    assetsInlineLimit: 0,
  },
  resolve: {
    alias: {
      "@": path.resolve(import.meta.dirname, "./src"),
    },
  },
  server: {
    // Same-origin API in development, the way nginx serves it in the container: the app
    // calls /api/v1 on its own origin and the dev server forwards it to the gateway
    proxy: {
      "/api": {
        target: process.env.GATEWAY_URL ?? "http://localhost:5000",
        changeOrigin: false,
        // The admin panel's live feed of orders is a WebSocket
        ws: true,
      },
    },
  },
});

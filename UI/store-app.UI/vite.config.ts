import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    // Same-origin API in development, the way nginx serves it in the container: the app
    // calls /api/v1 on its own origin and the dev server forwards it to the gateway
    proxy: {
      "/api": {
        target: process.env.GATEWAY_URL ?? "http://localhost:5000",
        changeOrigin: false,
      },
    },
  },
});

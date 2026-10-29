import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// In development the SPA calls the gateway through the Vite proxy, so the browser sees one origin,
// exactly like production where nginx serves the SPA and proxies /api to the gateway.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: process.env.GATEWAY_URL ?? 'http://localhost:5100', changeOrigin: true },
    },
  },
  build: {
    sourcemap: true,
  },
});

import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import basicSsl from '@vitejs/plugin-basic-ssl';

// Dev server (npm run dev) mirrors the nginx proxy used in Docker, so the frontend
// always calls relative URLs (/api, /notificationHub, /dashboard/api).
// Defaults target the ports published by docker-compose; override in .env.local
// (e.g. VITE_API_TARGET=http://localhost:5000 when running the API with dotnet run).
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const apiTarget = env.VITE_API_TARGET || 'http://localhost:5001';
  const dashboardTarget = env.VITE_DASHBOARD_TARGET || 'http://localhost:5002';

  return {
    plugins: [
      react(),
      // Self-signed HTTPS, so browser notifications also work from another LAN machine
      basicSsl()
    ],
    server: {
      port: 3000,
      host: true,
      proxy: {
        '/api': { target: apiTarget, changeOrigin: true },
        '/notificationHub': { target: apiTarget, changeOrigin: true, ws: true },
        '/dashboard': {
          target: dashboardTarget,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/dashboard/, '')
        }
      }
    }
  };
});

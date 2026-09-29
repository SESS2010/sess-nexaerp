import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// The NexaERP API has no CORS policy; the dev proxy forwards /api and /health
// so the browser sees a same-origin backend.
//
// Network access: `host: true` binds the dev server to 0.0.0.0 so other
// workstations can open http://<this-machine-ip>:5173. For a shared server,
// prefer `npm run build:api`, which places the built app in the API's wwwroot
// so the API serves it directly on http://<server-ip>:5000.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', '')
  const apiTarget = env.VITE_API_TARGET ?? 'http://localhost:5000'
  return {
    plugins: [react(), tailwindcss()],
    server: {
      host: true,
      port: 5173,
      strictPort: true,
      proxy: {
        '/api': { target: apiTarget, changeOrigin: true },
        '/health': { target: apiTarget, changeOrigin: true },
      },
    },
  }
})

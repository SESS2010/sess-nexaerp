import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// The NexaERP API has no CORS policy; the dev proxy forwards /api and /health
// so the browser sees a same-origin backend.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', '')
  const apiTarget = env.VITE_API_TARGET ?? 'http://localhost:5000'
  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        '/api': { target: apiTarget, changeOrigin: true },
        '/health': { target: apiTarget, changeOrigin: true },
      },
    },
  }
})

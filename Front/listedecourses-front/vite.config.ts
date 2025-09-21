
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

const HMR_HOST = process.env.VITE_HMR_HOST

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    host: true,
    hmr: HMR_HOST ? { host: HMR_HOST, clientPort: 5173 } : { clientPort: 5173 },
    proxy: {
      '/api': {
        target: 'https://localhost:7145',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})

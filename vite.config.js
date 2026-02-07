import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import fable from 'vite-plugin-fable'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [fable({ fsproj: './src/ElmishPaint.fsproj', jsx: 'automatic' }), react(), tailwindcss()],
  root: "./src",
  build: {
    outDir: "../dist",
  },
  test: {
    globals: true, // enables afterEach `cleanup` from RTL. Without this all components will stay mounted after each test
    include: ['**/*.{test,spec}.?(c|m|fs.)[jt]s?(x)'],
    environment: 'jsdom',
    setupFiles: ['./vitest-setup.ts'],
  }
})

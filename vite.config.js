import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import fable from 'vite-plugin-fable'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  base: '/ElmishPaint/',
  plugins: [
    fable({ fsproj: './src/ElmishPaint.fsproj', jsx: 'automatic' }),
    react(),
    tailwindcss()
  ],
  root: "./src",
  build: {
    outDir: "../dist",
  },
  test: {
    globals: true,
    include: ['**/*.{test,spec}.?(c|m|fs.)[jt]s?(x)'],
    environment: 'jsdom',
    setupFiles: ['./vitest-setup.ts'],
  }
})

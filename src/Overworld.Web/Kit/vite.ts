import react from '@vitejs/plugin-react';
import { defineConfig, type UserConfig } from 'vite';

export function page(): UserConfig {
  return defineConfig({
    plugins: [react()],
    base: '/',
    build: { outDir: 'dist', emptyOutDir: true, chunkSizeWarningLimit: 2000 },
  });
}

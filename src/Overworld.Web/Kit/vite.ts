import react from '@vitejs/plugin-react';
import { defineConfig, type PluginOption, type UserConfig } from 'vite';

const served = ['/ws', '/theme.css', '/blob', '/file', '/kit', '/icon.png'];

export function page(extra: PluginOption[] = []): UserConfig {
  const host = process.env.OVERWORLD_HOST;
  return defineConfig({
    plugins: [react(), ...extra],
    server: host ? { proxy: Object.fromEntries(served.map((path) => [path, { target: host, ws: path === '/ws' }])) } : undefined,
    base: '/',
    build: { outDir: 'dist', emptyOutDir: true, chunkSizeWarningLimit: 2000 },
  });
}

import { defineConfig } from 'vitest/config';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import babel from '@rolldown/plugin-babel';

// Igual que vite.config.ts, pero con React Compiler (doc: react.dev/learn/react-compiler/installation).
export default defineConfig({
  plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
  build: { outDir: 'dist-compiler' },
});

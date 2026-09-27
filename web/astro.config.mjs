// @ts-check
import { fileURLToPath } from 'node:url';
import { defineConfig, fontProviders } from 'astro/config';
import preact from '@astrojs/preact';

// Static site (Firebase Hosting); everything dynamic runs in the browser against Firebase and the Cloud Functions.
export default defineConfig({
  site: process.env.SITE_URL ?? 'https://bookknhom.web.app',
  output: 'static',
  trailingSlash: 'always',
  // Astro 7's default ('jsx') drops spaces between inline elements; keep normal HTML whitespace.
  compressHTML: true,
  integrations: [preact()],
  fonts: [
    {
      provider: fontProviders.fontsource(),
      name: 'Pixelify Sans',
      cssVariable: '--font-pixel',
      weights: [500, 700],
      styles: ['normal'],
      subsets: ['latin'],
      fallbacks: ['monospace'],
    },
    {
      provider: fontProviders.fontsource(),
      name: 'Nunito',
      cssVariable: '--font-body',
      weights: [400, 600, 700, 800],
      styles: ['normal'],
      subsets: ['latin'],
      fallbacks: ['sans-serif'],
    },
  ],
  vite: {
    resolve: {
      // The data contract shared with the Cloud Functions (one source of truth for both sides).
      alias: { '@shared': fileURLToPath(new URL('../firebase/functions/src/shared', import.meta.url)) },
    },
    server: { fs: { allow: ['..'] } },
  },
});

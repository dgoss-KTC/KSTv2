import { build } from '../../src/frontend/node_modules/esbuild/lib/main.js';
await build({ entryPoints: ['scripts/stage11-performance/verify-production.ts'], bundle: true, platform: 'node', format: 'esm', outfile: 'scripts/stage11-performance/captures/verify-production.mjs' });
await import('./captures/verify-production.mjs');

import { build } from '../../src/frontend/node_modules/esbuild/lib/main.js';
await build({ entryPoints: ['scripts/stage11-performance/verify-compact.ts'], bundle: true, platform: 'node', format: 'esm', outfile: 'scripts/stage11-performance/captures/verify-compact.mjs' });
await import('./captures/verify-compact.mjs');

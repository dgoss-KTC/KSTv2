import { build } from '../../src/frontend/node_modules/esbuild/lib/main.js';
import { createServer } from 'node:http';
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { spawn } from 'node:child_process';

const directory = resolve('scripts/stage11-performance/captures');
const name = process.argv[2] ?? '2140';
const virtual = process.argv[3] === 'virtual';
const production = name.endsWith('-production');
const payload = readFileSync(resolve(directory, `${name}-api.json`));
const controlName = name.replace(/-(compact|production)$/, '-control');
const control = name.endsWith('-compact') || production ? JSON.parse(readFileSync(resolve(directory, `${controlName}-api.json`))) : null;
await build({ entryPoints: [production ? 'scripts/stage11-performance/frontend-production-entry.tsx' : 'scripts/stage11-performance/frontend-entry.tsx'], bundle: true, outfile: resolve(directory, 'frontend.js'),
  define: { 'process.env.NODE_ENV': '"production"', 'import.meta.env.DEV': 'false', 'import.meta.env.VITE_BACKEND_URL': '"http://127.0.0.1:15402"' },
  alias: { 'react-dom/client': resolve('src/frontend/node_modules/react-dom/profiling.js') }, minify: true,
  plugins: virtual ? [{ name: 'offline-virtual-prototype', setup(build) {
    build.onLoad({ filter: /LongTermShortagesPanel\.tsx$/ }, async (args) => {
      let contents = readFileSync(args.path, 'utf8');
      const original = '<tbody>{rows.map((row) => <ShortageMatrixRow key={row.componentPart} row={row} onSelect={selectPart} />)}</tbody>';
      if (!contents.includes(original)) throw new Error('Production grid changed; review prototype transform.');
      contents = `import { VirtualBody } from '${resolve('scripts/stage11-performance/VirtualBody.tsx').replaceAll('\\', '/')}';\n` + contents.replace(original,
        '<VirtualBody rows={rows} render={(row) => <ShortageMatrixRow key={row.componentPart} row={row} onSelect={selectPart} />} />');
      contents = contents.replace('<tr aria-label={`${row.componentPart}', '<tr data-row="true" aria-label={`${row.componentPart}');
      return { contents, loader: 'tsx' };
    });
  } }] : [] });
const server = createServer(async (request, response) => {
  if (request.url === '/metrics') {
    let body = ''; for await (const chunk of request) body += chunk;
    writeFileSync(resolve(directory, `${name}${virtual ? '-virtual' : ''}-frontend-${Date.now()}.json`), body);
    console.log(body);
    if (JSON.parse(body).error) process.exitCode = 1;
    response.end('ok');
    setTimeout(() => { browser.kill(); server.close(); }, 100);
  } else if (request.url.startsWith('/detail?') && control) {
    const part = new URL(request.url, 'http://127.0.0.1').searchParams.get('part');
    response.setHeader('Content-Type', 'application/json');
    const row = control.rows.find(r => r.componentPart === part);
    response.end(JSON.stringify(production ? { snapshotId: control.snapshotId, componentPart: part,
      refreshDate: control.refreshDate, acquiredAtUtc: control.acquiredAtUtc, consistencyMode: control.consistencyMode,
      demandParentParts: row.demandParentParts, pastGrossRequirements: row.past.grossRequirements,
      overdueReceipts: row.past.overdueReceipts, adjustedOpeningQoh: row.past.projectedQoh,
      effectivePmCode: row.effectivePmCode, manufacturingLeadWorkingDays: row.manufacturingLeadWorkingDays,
      manufacturerItem: row.presentation?.manufacturerItem ?? null }
      : { confirmed: row, all: control.allReceiptsRows.find(r => r.componentPart === part) }));
  } else if (request.url === '/payload') { response.setHeader('Content-Type', 'application/json'); response.end(payload); }
  else if (request.url === '/frontend.js') { response.setHeader('Content-Type', 'text/javascript'); response.end(readFileSync(resolve(directory, 'frontend.js'))); }
  else if (request.url === '/frontend.css') { response.setHeader('Content-Type', 'text/css'); response.end(readFileSync(resolve(directory, 'frontend.css'))); }
  else { response.setHeader('Content-Type', 'text/html'); response.end('<html><head><link rel="stylesheet" href="/frontend.css"><style>#root{height:100vh;display:flex;flex-direction:column}.long-term-shortages{flex:1;min-height:0}</style></head><body><div id="root"></div><script src="/frontend.js"></script></body></html>'); }
});
await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const profile = resolve(directory, `edge-${name}-${Date.now()}`);
mkdirSync(profile);
const browser = spawn('C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', ['--headless=new', '--disable-gpu', '--no-first-run', '--disable-background-networking', '--window-size=1440,900', `--user-data-dir=${profile}`, `http://127.0.0.1:${server.address().port}`], { stdio: 'ignore' });
const timeout = setTimeout(() => { browser.kill(); server.close(); console.error('Browser profiling timed out'); process.exitCode = 1; }, 90000);
server.on('close', () => clearTimeout(timeout));

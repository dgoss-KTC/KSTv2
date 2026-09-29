import React, { Profiler } from '../../src/frontend/node_modules/react';
import { createRoot } from '../../src/frontend/node_modules/react-dom/profiling';
import { LongTermShortagesPanel } from '../../src/frontend/src/components/LongTermShortagesPanel';
import '../../src/frontend/src/index.css';

const results: unknown[] = [];
const commits: { phase: string; actualDuration: number }[] = [];
let requests = 0, detailRequests = 0;
const originalFetch = window.fetch.bind(window);
window.fetch = async (input, init) => {
  const url = String(input);
  if (url.includes('/projection-detail?')) {
    detailRequests++;
    const start = performance.now();
    const part = new URL(url).searchParams.get('componentPart');
    const response = await originalFetch(`/detail?part=${encodeURIComponent(part!)}`);
    const data = await response.json();
    results.push({ phase: 'cached-detail-replay', elapsedMs: performance.now() - start, requests, detailRequests });
    return { ok: true, json: async () => ({ ...data, snapshotId: '00000000-0000-0000-0000-000000000001' }) } as Response;
  }
  if (url.includes('/screen?')) {
    requests++;
    const start = performance.now();
    const response = await originalFetch('/payload', init);
    const text = await response.text();
    const received = performance.now();
    const data = JSON.parse(text);
    results.push({ phase: 'transfer-and-parse', transferMs: received - start, parseMs: performance.now() - received, bytes: new TextEncoder().encode(text).length });
    return { ok: true, json: async () => ({ ...data, snapshotId: '00000000-0000-0000-0000-000000000001' }) } as Response;
  }
  return new Response('{}', { status: 503 });
};
const start = performance.now();
createRoot(document.getElementById('root')!).render(<Profiler id="shortages" onRender={(_, phase, actualDuration) => commits.push({ phase, actualDuration })}>
  <LongTermShortagesPanel assignmentId="perf" snapshotId="00000000-0000-0000-0000-000000000001" />
</Profiler>);
async function frame() { await new Promise<void>(resolve => requestAnimationFrame(() => resolve())); }
async function measure() {
  for (let i = 0; i < 1200 && !document.querySelector('tbody'); i++) await new Promise(resolve => setTimeout(resolve, 50));
  await frame(); await frame();
  if (!document.querySelector('tbody')) throw new Error('Grid failed to load');
  results.push({ phase: 'initial', elapsedMs: performance.now() - start, rows: document.querySelectorAll('tbody tr').length, cells: document.querySelectorAll('tbody td').length, commits: [...commits], requests });
  const checkbox = (text: string) => [...document.querySelectorAll('label')].find(label => label.textContent?.includes(text))!.querySelector('input')!;
  for (const [phase, label] of [['show-all', 'Show All'], ['receipt-mode', 'Include Unconfirmed Receipts']]) {
    commits.length = 0;
    const begin = performance.now(); checkbox(label).click(); await frame(); await frame();
    results.push({ phase, elapsedMs: performance.now() - begin, commits: [...commits], requests });
  }
  const origin = document.querySelector('tbody button') as HTMLButtonElement;
  origin.click();
  for (let i = 0; i < 100 && !document.querySelector('.long-term-shortage-detail__parents dd')?.textContent?.includes('PARENT') && detailRequests === 0; i++) await new Promise(resolve => setTimeout(resolve, 20));
  await new Promise(resolve => setTimeout(resolve, 100)); await frame(); await frame();
  const hydrated = !document.body.textContent?.includes('Snapshot projection detail unavailable') && !document.body.textContent?.includes('Loading snapshot projection detail');
  if (!hydrated || detailRequests !== 1 || requests !== 1) throw new Error('Detail hydration/request-count gate failed');
  document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
  await frame(); await frame();
  results.push({ phase: 'drawer', hydrated, detailRequests, requests, focusRestored: document.activeElement === origin, closed: !document.querySelector('[role="dialog"]') });
  await originalFetch('/metrics', { method: 'POST', body: JSON.stringify(results) });
}
void measure().catch(error => originalFetch('/metrics', { method: 'POST', body: JSON.stringify({ error: String(error), results }) }));

import React, { Profiler } from '../../src/frontend/node_modules/react';
import { createRoot } from '../../src/frontend/node_modules/react-dom/profiling';
import { LongTermShortagesPanel } from '../../src/frontend/src/components/LongTermShortagesPanel';
import '../../src/frontend/src/index.css';
import { EMPTY_LONG_TERM_SHORTAGES_FILTERS } from '../../src/frontend/src/longTermShortages/longTermShortagesPresentation';
import { filterLongTermShortages } from './legacyScreenPresentation';
import { expandScreen } from './compact-adapter';
import type { LongTermShortageRow } from '../../src/frontend/src/api/longTermShortagesApi';

const results: unknown[] = [];
const commits: { phase: string; actualDuration: number }[] = [];
let requests = 0;
let loaded: { rows: LongTermShortageRow[]; allReceiptsRows: LongTermShortageRow[] };
let compact = false;
const originalFetch = window.fetch.bind(window);
window.addEventListener('error', event => results.push({ phase: 'browser-error', message: event.message }));
window.fetch = async (input, init) => {
  if (String(input).includes('long-term-shortages') && !String(input).includes('purchasing')) {
    requests++;
    const start = performance.now();
    const response = await originalFetch('/payload', init);
    const text = await response.text();
    const received = performance.now();
    const wire = JSON.parse(text);
    const parseMs = performance.now() - received;
    const expandStart = performance.now();
    compact = Boolean(wire.components);
    const parsed = compact ? expandScreen(wire) : wire;
    loaded = parsed;
    const expandMs = performance.now() - expandStart;
    const filterStart = performance.now();
    filterLongTermShortages(parsed.rows, EMPTY_LONG_TERM_SHORTAGES_FILTERS);
    results.push({ phase: 'transfer-and-parse', transferMs: received - start, parseMs, expandMs, filterMs: performance.now() - filterStart, bytes: new TextEncoder().encode(text).length });
    return { ok: true, json: async () => parsed } as Response;
  }
  return new Response('{}', { status: 503 });
};
// Existing client accepts an explicit local development backend port.
const start = performance.now();
const root = createRoot(document.getElementById('root')!);
root.render(<Profiler id="shortages" onRender={(_, phase, actualDuration) => commits.push({ phase, actualDuration })}>
  <LongTermShortagesPanel assignmentId="perf" snapshotId="00000000-0000-0000-0000-000000000001" />
</Profiler>);
async function frame() { await new Promise<void>(resolve => requestAnimationFrame(() => resolve())); }
async function measure() {
  for (let i = 0; i < 1200 && !document.querySelector('tbody'); i++) await new Promise(resolve => setTimeout(resolve, 50));
  await frame();
  const grid = document.querySelector('.long-term-shortages__grid') as HTMLElement;
  if (!grid) throw new Error('Grid did not load');
  const layoutStart = performance.now();
  const height = grid.scrollHeight;
  const width = grid.scrollWidth;
  const layoutMs = performance.now() - layoutStart;
  await frame();
  results.push({ phase: 'initial', elapsedMs: performance.now() - start, layoutMs, height, width,
    rows: document.querySelectorAll('tbody tr').length, cells: document.querySelectorAll('tbody td').length, commits: [...commits], requests });
  commits.length = 0;
  const control = [...document.querySelectorAll('label')].find(label => label.textContent?.includes('Show All'))!.querySelector('input')!;
  const updateStart = performance.now();
  control.click();
  await frame(); await frame();
  results.push({ phase: 'show-all', elapsedMs: performance.now() - updateStart, rows: document.querySelectorAll('tbody tr').length, commits: [...commits], requests });
  const failures: string[] = [];
  const section = document.querySelector('.long-term-shortages') as HTMLElement;
  for (const [width, scale] of [[1350, 1], [1100, 1], [1100, 1.25], [1000, 1.5]]) {
    section.style.width = `${width}px`;
    document.body.style.zoom = String(scale);
    grid.scrollLeft = 380; grid.scrollTop = 600;
    await frame(); await frame();
    const headers = [...document.querySelectorAll('thead th')];
    const row = document.querySelector('tbody tr:has(button)')!;
    const cells = [...row.querySelectorAll('td')];
    for (let i = 0; i < 7; i++) {
      const h = headers[i].getBoundingClientRect(), c = cells[i].getBoundingClientRect();
      if (Math.abs(h.left - c.left) > 1 || Math.abs(h.width - c.width) > 1) failures.push(`frozen-${i}-${width}-${scale}`);
    }
    if (Math.abs(headers[20].getBoundingClientRect().top - headers[0].getBoundingClientRect().top) > 1) failures.push('sticky-header');
    if (grid.scrollWidth <= grid.clientWidth || getComputedStyle(grid).overflowX !== 'auto') failures.push('horizontal-scroll');
  }
  section.style.width = ''; document.body.style.zoom = '1'; grid.scrollTop = 0; grid.scrollLeft = 0;
  await frame(); await frame();
  // Focus an offscreen row then scroll away: unmounting a focused row is a virtualization blocker.
  const focusOrigin = document.querySelector('tbody button') as HTMLButtonElement;
  focusOrigin.focus(); grid.scrollTop = grid.scrollHeight;
  await frame(); await frame();
  const offscreenFocusPreserved = document.activeElement === focusOrigin && focusOrigin.isConnected;
  grid.scrollTop = 0; await frame(); await frame();
  results.push({ phase: 'prototype-geometry', failures, offscreenFocusPreserved, requests });
  commits.length = 0;
  // Model a selected-component cached-snapshot detail response before opening the unchanged
  // drawer. This is an offline bridge, not a production endpoint or a cache-hit latency claim.
  if (compact) {
    const part = (document.querySelector('tbody button') as HTMLButtonElement).textContent!;
    const detailStart = performance.now();
    const detail = await (await originalFetch(`/detail?part=${encodeURIComponent(part)}`)).json();
    Object.assign(loaded.rows.find(r => r.componentPart === part)!, detail.confirmed);
    Object.assign(loaded.allReceiptsRows.find(r => r.componentPart === part)!, detail.all);
    results.push({ phase: 'cached-detail-replay', elapsedMs: performance.now() - detailStart, requests });
  }
  const drawerStart = performance.now();
  (document.querySelector('tbody button') as HTMLButtonElement).click();
  await new Promise(resolve => setTimeout(resolve, 100));
  await frame(); await frame();
  results.push({ phase: 'drawer-error-path', elapsedMs: performance.now() - drawerStart, commits: [...commits], requests });
  document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
  await frame(); await frame();
  results.push({ phase: 'drawer-focus', restored: document.activeElement?.matches('tbody button'), closed: !document.querySelector('[role="dialog"]'), requests });
  const toggle = [...document.querySelectorAll('label')].find(label => label.textContent?.includes('Include Unconfirmed Receipts'))!.querySelector('input')!;
  const modeStart = performance.now(); toggle.click(); await frame(); await frame();
  results.push({ phase: 'receipt-mode', elapsedMs: performance.now() - modeStart, requests });
  await originalFetch('/metrics', { method: 'POST', body: JSON.stringify(results) });
}
void measure().catch(error => originalFetch('/metrics', { method: 'POST', body: JSON.stringify({ error: String(error), results }) }));

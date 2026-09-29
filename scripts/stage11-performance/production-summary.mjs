import { readFileSync, readdirSync, statSync } from 'node:fs';
const directory = 'scripts/stage11-performance/captures';
const files = readdirSync(directory);
const median = values => { const sorted = [...values].sort((a, b) => a - b); return (sorted[Math.floor((sorted.length - 1) / 2)] + sorted[Math.floor(sorted.length / 2)]) / 2; };
for (const phase of ['production-contract', 'production-dense']) {
  const file = files.filter(f => new RegExp(`^${phase}-\\d.*jsonl$`).test(f)).sort().at(-1);
  const rows = readFileSync(`${directory}/${file}`, 'utf8').trim().split('\n').map(JSON.parse).filter(r => r.run > 0);
  for (const workspace of [...new Set(rows.map(r => r.workspace))]) {
    const selected = rows.filter(r => r.workspace === workspace);
    if (phase === 'production-contract') console.log(JSON.stringify({ phase, workspace, mappingMs: median(selected.map(r => r.mappingMs)), serializationMs: median(selected.map(r => r.serializationMs)), allocation: median(selected.map(r => r.mappingAllocatedBytes)), bytes: selected[0].bytes }));
    else for (const dense of [false, true]) {
      const samples = selected.filter(r => r.dense === dense);
      console.log(JSON.stringify({ phase, workspace, dense, warmMs: median(samples.map(r => r.elapsedMs)), allocation: median(samples.map(r => r.bytesAllocated)) }));
    }
  }
}
for (const file of files.filter(f => /-live-.*-production-api.json$/.test(f))) console.log(JSON.stringify({ phase: 'live-payload', workspace: file.split('-')[0], beforeBytes: statSync(`${directory}/${file.replace('-production-', '-control-')}`).size, afterBytes: statSync(`${directory}/${file}`).size }));

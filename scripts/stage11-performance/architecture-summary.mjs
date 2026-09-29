import { readFileSync, readdirSync } from 'node:fs';
const directory = 'scripts/stage11-performance/captures';
const median = values => { const v = values.sort((a,b) => a-b); return (v[Math.floor(v.length/2)] + v[Math.ceil(v.length/2)-1])/2; };
for (const mode of ['algorithm-prototypes', 'contract-prototype']) {
  const file = readdirSync(directory).filter(f => f.startsWith(`architecture-${mode}-`)).sort().at(-1);
  const rows = readFileSync(`${directory}/${file}`, 'utf8').trim().split(/\r?\n/).map(JSON.parse);
  for (const workspace of [...new Set(rows.map(r => r.workspace))]) {
    for (const variant of mode === 'algorithm-prototypes' ? ['indexed', 'dense', 'sparse'] : [false, true]) {
      const selected = rows.filter(r => r.workspace === workspace && (r.variant ?? r.compact) === variant);
      const warm = selected.filter(r => r.run > 0);
      const fields = mode === 'algorithm-prototypes' ? ['elapsedMs', 'bytesAllocated'] : ['mappingMs', 'serializationMs', 'bytes', 'mappingAllocatedBytes'];
      console.log(JSON.stringify({ mode, workspace, variant, first: Object.fromEntries(fields.map(f => [f, selected[0][f]])), warm: Object.fromEntries(fields.map(f => [f, median(warm.map(r => r[f]))])) }));
    }
  }
}

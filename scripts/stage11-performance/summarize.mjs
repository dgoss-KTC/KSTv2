import { readFileSync, readdirSync } from 'node:fs';
const directory = 'scripts/stage11-performance/captures';
for (const file of readdirSync(directory).filter(f => f.endsWith('-sql.jsonl'))) {
  const rows = readFileSync(`${directory}/${file}`, 'utf8').trim().split('\n').map(JSON.parse);
  for (const run of [0, 1]) {
    for (const kind of ['bom', 'facts', 'presentation', 'drawer-po']) {
      const group = rows.filter(r => r.run === run && r.query.startsWith(kind));
      const reads = {};
      let cpu = 0, serverMs = 0;
      for (const r of group) for (const message of r.messages) {
        const table = message.match(/Table '([^']+)'.*?logical reads (\d+)/);
        if (table) reads[table[1]] = (reads[table[1]] ?? 0) + Number(table[2]);
        const time = message.match(/Execution Times:\s+CPU time = (\d+) ms,\s+elapsed time = (\d+) ms/);
        if (time) { cpu += Number(time[1]); serverMs += Number(time[2]); }
      }
      console.log(JSON.stringify({ file, run, kind, queries: group.length, ms: group.reduce((n, r) => n + r.elapsedMs, 0),
        rows: group.reduce((n, r) => n + r.rows, 0), bytes: group.reduce((n, r) => n + r.bytesReceived, 0), cpu, serverMs, reads }));
    }
  }
}
console.log('Plan evidence: SHOWPLAN denied (SQL error 262). Initial RPC attempt returned empty files; its available=true output is INVALID and superseded.');

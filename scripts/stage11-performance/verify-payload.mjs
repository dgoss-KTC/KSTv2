import { readFileSync } from 'node:fs';
import assert from 'node:assert/strict';
const directory = 'scripts/stage11-performance/captures';
for (const name of ['2140', '2141', '2142']) {
  const before = JSON.parse(readFileSync(`${directory}/${name}-api.json`, 'utf8'));
  const after = JSON.parse(readFileSync(`${directory}/${name}-screen-api.json`, 'utf8'));
  assert.equal(after.evidenceIncluded, false);
  delete after.evidenceIncluded;
  delete before.evidenceIncluded;
  for (const mode of ['rows', 'allReceiptsRows']) {
    for (const row of before[mode]) row.evidence = [];
  }
  assert.deepEqual(after, before);
  console.log(JSON.stringify({ workspace: name, allNonEvidenceFieldsEquivalent: true }));
}

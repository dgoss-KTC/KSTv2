import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import {
  businessWeekOf,
  clearableKeys,
  decodeMassUpdaterBytes,
  exportedAdjustments,
  exportRequestEntries,
  MassUpdaterFileError,
  parseMassUpdaterCsv,
  verifyAgainstStaged,
} from './massUpdaterExport';
import { adjustmentKey, type AdjustmentEntries } from './planningAdjustments';

const goldenBytes = readFileSync(resolve(process.cwd(), '../../docs/reference/Stage14Fixtures/MpsAdjustments.expected.csv'));
const golden64 = goldenBytes.toString('base64');
const b64 = (text: string) => Buffer.from(text, 'utf8').toString('base64');

const entry = (part: string, weekLabel: string, text: string) => ({ part, weekLabel, text });
const entries = (...list: ReturnType<typeof entry>[]): AdjustmentEntries =>
  Object.fromEntries(list.map(e => [adjustmentKey(e.part, e.weekLabel), e]));

describe('Mass Updater file parsing (preview is parsed from the actual bytes)', () => {
  it('parses the accepted golden file into its rows, in file order, with Part, Qty and Date as written', () => {
    const rows = parseMassUpdaterCsv(decodeMassUpdaterBytes(golden64));

    expect(rows.map(r => [r.part, r.quantity, r.dateText, r.weekLabel])).toEqual([
      ['P-2', '-100', '9/28/2026', '2026-09-28'],
      ['P-1', '-50', '9/14/2026', '2026-09-14'],
      ['P-1', '10', '9/21/2026', '2026-09-21'],
      ['P-1', '20', '10/5/2026', '2026-10-05'],
    ]);
  });

  it('handles RFC 4180 quoting without altering the part text', () => {
    const rows = parseMassUpdaterCsv('"A,B",5,9/14/2026\r\n"Q""1",-5,10/5/2026\r\n"=X",7,11/2/2026\r\n');

    expect(rows.map(r => r.part)).toEqual(['A,B', 'Q"1', '=X']);
  });

  it.each([
    ['empty file', ''],
    ['header row', 'Part,Qty,Date\r\n'],
    ['missing final CRLF', 'P-1,5,9/14/2026'],
    ['LF only', 'P-1,5,9/14/2026\n'],
    ['leading plus', 'P-1,+5,9/14/2026\r\n'],
    ['zero quantity', 'P-1,0,9/14/2026\r\n'],
    ['fractional quantity', 'P-1,1.5,9/14/2026\r\n'],
    ['padded date', 'P-1,5,09/14/2026\r\n'],
    ['ISO date', 'P-1,5,2026-09-14\r\n'],
    ['two columns', 'P-1,5\r\n'],
    ['four columns', 'P-1,5,9/14/2026,x\r\n'],
    ['blank line', 'P-1,5,9/14/2026\r\n\r\n'],
    ['unterminated quote', '"P-1,5,9/14/2026\r\n'],
  ])('rejects a malformed file: %s', (_name, text) => {
    expect(() => parseMassUpdaterCsv(text)).toThrow(MassUpdaterFileError);
  });

  it('rejects a byte-order mark and invalid UTF-8 instead of previewing them', () => {
    expect(() => decodeMassUpdaterBytes(Buffer.from([0xef, 0xbb, 0xbf, 0x50]).toString('base64'))).toThrow(/byte-order mark/);
    expect(() => decodeMassUpdaterBytes(Buffer.from([0xc3, 0x28]).toString('base64'))).toThrow(/UTF-8/);
    expect(() => decodeMassUpdaterBytes('***')).toThrow(MassUpdaterFileError);
    expect(decodeMassUpdaterBytes(b64('é-ü,1,9/14/2026\r\n'))).toBe('é-ü,1,9/14/2026\r\n');
  });
});

describe('Mass Updater export agreement with the staged adjustments', () => {
  const staged = entries(
    entry('P-1', '2026-09-14', '-50'), entry('P-1', '2026-09-21', '10'), entry('p-1', '2026-10-05', '020'),
    entry('P-2', '2026-09-28', '-100'), entry('P-2', '2026-09-21', '0'), entry('P-2', '2026-10-12', ''),
  );

  it('exports only nonzero valid proposals and sends every entry as typed', () => {
    expect(Object.values(exportedAdjustments(staged)).map(e => e.text)).toEqual(['-50', '10', '020', '-100']);
    expect(exportRequestEntries(staged)).toHaveLength(6);
    expect(exportRequestEntries(staged)[0]).toEqual({ part: 'P-1', weekLabel: '2026-09-14', adjustment: '-50' });
  });

  it('accepts a file with exactly the staged cells and signed quantities (case-insensitive part, canonical zero-padding)', () => {
    const rows = parseMassUpdaterCsv('P-2,-100,9/28/2026\r\nP-1,-50,9/14/2026\r\nP-1,10,9/21/2026\r\nP-1,20,10/5/2026\r\n');

    expect(verifyAgainstStaged(rows, exportedAdjustments(staged), 4)).toBeNull();
  });

  it.each([
    ['a missing row', 'P-2,-100,9/28/2026\r\nP-1,-50,9/14/2026\r\nP-1,10,9/21/2026\r\n', 4],
    ['an extra row', 'P-2,-100,9/28/2026\r\nP-1,-50,9/14/2026\r\nP-1,10,9/21/2026\r\nP-1,20,10/5/2026\r\nP-3,1,9/14/2026\r\n', 4],
    ['a different quantity', 'P-2,-100,9/28/2026\r\nP-1,-50,9/14/2026\r\nP-1,11,9/21/2026\r\nP-1,20,10/5/2026\r\n', 4],
    ['a different date', 'P-2,-100,9/28/2026\r\nP-1,-50,9/14/2026\r\nP-1,10,9/21/2026\r\nP-1,20,10/12/2026\r\n', 4],
    ['a duplicate row', 'P-2,-100,9/28/2026\r\nP-1,-50,9/14/2026\r\nP-1,10,9/21/2026\r\nP-1,10,9/21/2026\r\n', 4],
    ['a wrong reported row count', 'P-2,-100,9/28/2026\r\nP-1,-50,9/14/2026\r\nP-1,10,9/21/2026\r\nP-1,20,10/5/2026\r\n', 5],
  ])('rejects a file with %s', (_name, text, count) => {
    expect(verifyAgainstStaged(parseMassUpdaterCsv(text), exportedAdjustments(staged), count)).not.toBeNull();
  });
});

describe('Clearing exported adjustments', () => {
  const key = (part: string, week: string) => adjustmentKey(part, week);

  it('clears only entries that are still the exported entry', () => {
    const at = entries(entry('P-1', '2026-09-14', '-50'), entry('P-1', '2026-09-21', '10'), entry('P-2', '2026-09-28', '-100'));
    const exported = exportedAdjustments(at);
    const now: Record<string, ReturnType<typeof entry>> = {
      ...at,
      [key('P-1', '2026-09-21')]: entry('P-1', '2026-09-21', '11'),    // edited since: kept
      [key('P-3', '2026-09-14')]: entry('P-3', '2026-09-14', '5'),     // added since: kept
    };
    delete now[key('P-2', '2026-09-28')];                               // removed since: nothing to clear

    expect(clearableKeys(now, exported)).toEqual([key('P-1', '2026-09-14')]);
    expect(clearableKeys(entries(), exported)).toEqual([]);
  });

  it('keeps an entry edited and then edited back to the exported text, and one removed and entered again', () => {
    const at = entries(entry('P-1', '2026-09-14', '10'), entry('P-2', '2026-09-28', '10'), entry('P-3', '2026-09-14', '10'));
    const exported = exportedAdjustments(at);
    const now = {
      ...at,
      [key('P-1', '2026-09-14')]: entry('P-1', '2026-09-14', '10'),    // 10 -> 20 -> 10: a new entry with the same text
      [key('P-2', '2026-09-28')]: entry('P-2', '2026-09-28', '10'),    // removed, then entered again as 10
    };

    expect(clearableKeys(now, exported)).toEqual([key('P-3', '2026-09-14')]);   // only the untouched entry
  });
});
describe('Business week of the loaded workbook', () => {
  it('maps a date to the Sunday that starts its Sunday-Saturday business week', () => {
    expect(businessWeekOf('2026-09-16')).toBe('2026-09-13');
    expect(businessWeekOf('2026-09-13')).toBe('2026-09-13');
    expect(businessWeekOf('2026-09-19')).toBe('2026-09-13');
    expect(businessWeekOf('2026-09-20')).toBe('2026-09-20');
    expect(businessWeekOf('nonsense')).toBeNull();
  });
});

/**
 * Stage 14.6 Mass Updater export helpers (pure). The preview shown to the user is parsed from the actual
 * bytes the backend generated, so what is reviewed is what is saved. Nothing here converts quantities to
 * JavaScript numbers.
 */
import { currentBusinessWeekStart } from '../hooks/usePlanningWorkbook';
import { adjustmentKey, parseAdjustment, type AdjustmentEntries, type AdjustmentEntry } from './planningAdjustments';

/** One row of the generated file: the Part, Qty (signed adjustment to existing MPS) and Date fields. */
export interface MassUpdaterRow {
  part: string;
  /** Signed whole-number text exactly as written in the file (no leading '+'). */
  quantity: string;
  /** The Date field exactly as written in the file (M/d/yyyy). */
  dateText: string;
  /** The same date as ISO yyyy-MM-dd (the Monday week label). */
  weekLabel: string;
}

export class MassUpdaterFileError extends Error {}

/** Decodes the file bytes as strict UTF-8 without a byte-order mark. */
export function decodeMassUpdaterBytes(contentBase64: string): string {
  let bytes: Uint8Array;
  try {
    const binary = atob(contentBase64);
    bytes = Uint8Array.from(binary, c => c.charCodeAt(0));
  } catch {
    throw new MassUpdaterFileError('The prepared file content could not be decoded.');
  }
  if (bytes.length >= 3 && bytes[0] === 0xef && bytes[1] === 0xbb && bytes[2] === 0xbf) {
    throw new MassUpdaterFileError('The prepared file starts with a byte-order mark.');
  }
  try {
    return new TextDecoder('utf-8', { fatal: true, ignoreBOM: true }).decode(bytes);
  } catch {
    throw new MassUpdaterFileError('The prepared file is not valid UTF-8.');
  }
}

/** Splits RFC 4180 records (quoted fields, doubled quotes); every record, including the last, ends with CRLF. */
function records(text: string): string[][] {
  const result: string[][] = [];
  let record: string[] = [];
  let field = '';
  let quoted = false;
  let fieldStarted = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (quoted) {
      if (c === '"') {
        if (text[i + 1] === '"') { field += '"'; i++; } else quoted = false;
      } else field += c;
      continue;
    }
    if (c === '"' && !fieldStarted) { quoted = true; fieldStarted = true; continue; }
    if (c === ',') { record.push(field); field = ''; fieldStarted = false; continue; }
    if (c === '\r' && text[i + 1] === '\n') { record.push(field); result.push(record); record = []; field = ''; fieldStarted = false; i++; continue; }
    if (c === '\r' || c === '\n' || c === '"') throw new MassUpdaterFileError('The prepared file is not valid CSV.');
    field += c;
    fieldStarted = true;
  }
  if (quoted || record.length > 0 || field !== '' || fieldStarted) {
    throw new MassUpdaterFileError('The prepared file does not end with a complete row.');
  }
  return result;
}

/** Parses the generated Mass Updater CSV (no header; Part, Qty, Date) into its rows, in file order. */
export function parseMassUpdaterCsv(text: string): MassUpdaterRow[] {
  const parsed = records(text);
  if (parsed.length === 0) throw new MassUpdaterFileError('The prepared file has no rows.');
  return parsed.map(fields => {
    if (fields.length !== 3) throw new MassUpdaterFileError('The prepared file has a row that is not Part, Qty, Date.');
    const [part, quantity, dateText] = fields;
    const date = /^([1-9]|1[0-2])\/([1-9]|[12]\d|3[01])\/(\d{4})$/.exec(dateText);
    if (!part || !/^-?[1-9][0-9]*$/.test(quantity) || !date) {
      throw new MassUpdaterFileError('The prepared file has a row with an invalid Part, Qty or Date.');
    }
    const month = String(Number(date[1])).padStart(2, '0');
    const day = String(Number(date[2])).padStart(2, '0');
    return { part, quantity, dateText, weekLabel: `${date[3]}-${month}-${day}` };
  });
}

/**
 * The session adjustments that will be exported, as cell key → the staged entry object itself (nonzero valid
 * proposals only). Entry objects are immutable and a new object is created every time an entry is created,
 * edited or restored, so the object is the entry's identity: it distinguishes an untouched exported entry
 * from one that was edited (even back to the same text) or removed and entered again.
 */
export function exportedAdjustments(entries: AdjustmentEntries): Record<string, AdjustmentEntry> {
  const exported: Record<string, AdjustmentEntry> = {};
  for (const [key, entry] of Object.entries(entries)) {
    if (parseAdjustment(entry.text).kind === 'value') exported[key] = entry;
  }
  return exported;
}

/** Request entries: every staged entry as typed. The backend ignores no-ops and rejects anything invalid. */
export function exportRequestEntries(entries: AdjustmentEntries) {
  return Object.values(entries).map(entry => ({ part: entry.part, weekLabel: entry.weekLabel, adjustment: entry.text }));
}

/**
 * Checks that the generated file contains exactly the staged proposals (same cells, same signed
 * quantities, in a sensible count). A mismatch means the file must not be offered for saving.
 */
export function verifyAgainstStaged(rows: readonly MassUpdaterRow[], exported: Readonly<Record<string, AdjustmentEntry>>, rowCount: number): string | null {
  const keys = Object.keys(exported);
  if (rows.length !== rowCount || rows.length !== keys.length) return 'The prepared file does not contain the same number of rows as the staged adjustments.';
  const seen = new Set<string>();
  for (const row of rows) {
    const key = adjustmentKey(row.part, row.weekLabel);
    const staged = exported[key];
    if (staged === undefined || seen.has(key)) return 'The prepared file contains a row that is not a staged adjustment.';
    const parsed = parseAdjustment(staged.text);
    if (parsed.kind !== 'value' || parsed.value !== row.quantity) return 'The prepared file quantities do not match the staged adjustments.';
    seen.add(key);
  }
  return null;
}

/**
 * Keys of session entries that can be cleared after a saved export: only entries that are still the very
 * entry that was exported (same identity, see exportedAdjustments). An entry edited since preparation
 * (even back to the exported text), or removed and entered again, is later work and is never cleared.
 * Undo of such an edit restores the original entry object, i.e. the exported state itself.
 */
export function clearableKeys(current: AdjustmentEntries, exported: Readonly<Record<string, AdjustmentEntry>>): string[] {
  return Object.keys(exported).filter(key => current[key] === exported[key]);
}

/** Sunday start (yyyy-MM-dd) of the business week containing an ISO date (the workbook's "today"). */
export function businessWeekOf(isoDate: string): string | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(isoDate);
  return match ? currentBusinessWeekStart(new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]))) : null;
}

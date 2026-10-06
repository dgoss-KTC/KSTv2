/**
 * Stage 14.5 Planning adjustment staging: pure, exact-decimal calculations on top of the accepted 14.4
 * workbook. Mirrors the accepted Domain rules (ledger §4–§6):
 * - Adjustments are signed whole numbers: optional '-' followed by ASCII digits. '+', decimals,
 *   exponents, grouping and whitespace are rejected; blank, 0 and -0 are no-ops (not proposals).
 * - Proposed MPS = existing MPS + adjustment; Adjusted MPS $ = Proposed MPS × the same selected price
 *   (null when unpriced). Falldown is not adjustable: its proposed supply equals existing supply.
 * - Adj On Hand = Est. On Hand + cumulative visible adjustments (same Falldown seed).
 * - A negative Proposed MPS is invalid; zero is valid. Negative inventory never invalidates.
 * Source quantities and prices are never rounded and never converted to JavaScript Number.
 */
import { add, multiply, sumDecimals } from '../shared/exactDecimal';
import {
  partQuarterSummaries,
  type PartQuarterSummary,
  type PlanningBucket,
  type PlanningPart,
  type PlanningQuarter,
  type PlanningWorkbook,
  type WorkspaceFlowTotals,
} from './planningWorkbookSummary';

/** .NET decimal maximum: larger digit strings fail the Domain parser and are invalid here too. */
const MAX_ADJUSTMENT = 79228162514264337593543950335n;

export type AdjustmentParse =
  | { kind: 'value'; value: string }
  | { kind: 'noop' }
  | { kind: 'invalid-number' }
  | { kind: 'not-whole-number' };

export function parseAdjustment(text: string): AdjustmentParse {
  if (text === '') return { kind: 'noop' };
  const negative = text.startsWith('-');
  const digits = negative ? text.slice(1) : text;
  const point = digits.indexOf('.');
  if (point > 0 && point < digits.length - 1 && /^[0-9]+$/.test(digits.slice(0, point)) && /^[0-9]+$/.test(digits.slice(point + 1))) {
    return { kind: 'not-whole-number' };
  }
  if (!/^[0-9]+$/.test(digits)) return { kind: 'invalid-number' };
  const magnitude = BigInt(digits);
  if (magnitude > MAX_ADJUSTMENT) return { kind: 'invalid-number' };
  if (magnitude === 0n) return { kind: 'noop' };
  return { kind: 'value', value: `${negative ? '-' : ''}${magnitude.toString()}` };
}

/** A staged adjustment, keyed by part and Monday week label (never by a column index). */
export interface AdjustmentEntry { part: string; weekLabel: string; text: string }
export type AdjustmentEntries = Readonly<Record<string, AdjustmentEntry>>;

export function adjustmentKey(part: string, weekLabel: string): string {
  return `${part.toUpperCase()}\u0000${weekLabel}`;
}

export type EntryIssue =
  | 'invalid-number'
  | 'not-whole-number'
  | 'negative-result'
  | 'week-passed'
  | 'part-out-of-scope';

/**
 * Why a well-formed entry is not shown in the loaded workbook. Owner decision (2026-10-06): being
 * hidden is a display state, not a business issue, and does not by itself prevent export. The entry's
 * MPS baseline is not available here, so it is neither validated nor reported valid by the frontend;
 * export validation against the full loaded MPS snapshot (Stage 14.6) decides its eligibility.
 */
export type EntryHidden = 'outside-horizon' | 'beyond-maximum';

export interface EntryEvaluation {
  key: string;
  part: string;
  weekLabel: string;
  text: string;
  /** Canonical signed value; null for no-ops and invalid text. */
  value: string | null;
  noop: boolean;
  /** True when the entry's week is shown in the current workbook. */
  visible: boolean;
  /** Loaded MPS for the entry's cell; null when unavailable (not shown, passed or out of scope), distinct from "0". */
  currentMps: string | null;
  proposedMps: string | null;
  issue: EntryIssue | null;
  /** Set for a valid nonzero adjustment whose week is not in the loaded workbook (baseline unchecked). */
  hidden: EntryHidden | null;
}

export interface AdjustedBucket extends PlanningBucket {
  adjustment: string;
  proposedMps: string;
  /** Null when the part is unpriced. */
  adjustedMpsDollars: string | null;
  adjustedOnHand: string;
  /** Raw input text for weekly cells (falldown: always ''). */
  input: string;
  inputIssue: EntryIssue | null;
}

export interface AdjustedPart extends Omit<PlanningPart, 'falldown' | 'weeks'> {
  falldown: AdjustedBucket;
  weeks: AdjustedBucket[];
}

export interface PlanningEvaluation {
  parts: AdjustedPart[];
  /** Every entry with input text (including no-ops), in workbook part order then week order. */
  entries: EntryEvaluation[];
  /** Nonzero, syntactically valid adjustments (proposals). */
  stagedCount: number;
  /** Entries with a business or syntax issue (hidden entries are not issues). */
  issueCount: number;
  /** Proposals not shown in the loaded workbook, whose MPS baseline is not checked here. */
  hiddenCount: number;
  /**
   * True when at least one proposal exists and no entry has an issue. Hidden proposals do not make it
   * false, and it does not claim their baselines were validated: Stage 14.6 export validates every
   * entry against the full loaded MPS snapshot and fails closed.
   */
  isValid: boolean;
}

const plus = (a: string, b: string) => add(a, b) as string;

function weeksBetween(fromIso: string, toIso: string): number {
  const at = (iso: string) => { const [y, m, d] = iso.split('-').map(Number); return Date.UTC(y, m - 1, d); };
  return Math.round((at(toIso) - at(fromIso)) / (7 * 86_400_000));
}

/** Evaluates staged adjustments against the loaded workbook. With no workbook nothing can be evaluated. */
export function evaluatePlanning(workbook: PlanningWorkbook, entries: AdjustmentEntries): PlanningEvaluation {
  const partIndex = new Map(workbook.parts.map((p, i) => [p.part.toUpperCase(), i]));
  const weekIndex = new Map(workbook.weekLabels.map((label, i) => [label, i]));
  const firstWeek = workbook.weekLabels[0];
  const maximum = Number(workbook.maximumHorizonWeeks);

  const parsed = Object.entries(entries).map(([key, entry]) => ({ key, entry, parse: parseAdjustment(entry.text) }));
  const byCell = new Map(parsed.map(p => [adjustmentKey(p.entry.part, p.entry.weekLabel), p]));

  const parts: AdjustedPart[] = workbook.parts.map(part => {
    const price = part.isPriced && part.selectedPrice !== null ? part.selectedPrice : null;
    const dollars = (quantity: string) => (price === null ? null : multiply(quantity, price));
    const falldown: AdjustedBucket = {
      ...part.falldown, adjustment: '0', proposedMps: part.falldown.mps, adjustedMpsDollars: dollars(part.falldown.mps),
      adjustedOnHand: part.falldown.estimatedOnHand, input: '', inputIssue: null,
    };
    let cumulative = '0';
    const weeks = part.weeks.map((bucket, i) => {
      const cell = byCell.get(adjustmentKey(part.part, workbook.weekLabels[i]));
      const adjustment = cell?.parse.kind === 'value' ? cell.parse.value : '0';
      cumulative = plus(cumulative, adjustment);
      const proposedMps = plus(bucket.mps, adjustment);
      const syntaxIssue = cell && (cell.parse.kind === 'invalid-number' || cell.parse.kind === 'not-whole-number') ? cell.parse.kind : null;
      return {
        ...bucket, adjustment, proposedMps, adjustedMpsDollars: dollars(proposedMps),
        adjustedOnHand: plus(bucket.estimatedOnHand, cumulative), input: cell?.entry.text ?? '',
        inputIssue: syntaxIssue ?? (proposedMps.startsWith('-') ? 'negative-result' as const : null),
      };
    });
    return { ...part, falldown, weeks };
  });

  const evaluations: EntryEvaluation[] = parsed.map(({ key, entry, parse }) => {
    const base = { key, part: entry.part, weekLabel: entry.weekLabel, text: entry.text,
      value: parse.kind === 'value' ? parse.value : null, noop: parse.kind === 'noop' };
    const p = partIndex.get(entry.part.toUpperCase());
    const w = weekIndex.get(entry.weekLabel);
    const visible = p !== undefined && w !== undefined;
    const bucket = visible ? parts[p].weeks[w] : null;
    let issue: EntryIssue | null = null;
    let hidden: EntryHidden | null = null;
    if (parse.kind === 'invalid-number' || parse.kind === 'not-whole-number') issue = parse.kind;
    else if (parse.kind === 'value') {
      if (p === undefined) issue = 'part-out-of-scope';
      else if (firstWeek !== undefined && entry.weekLabel < firstWeek) issue = 'week-passed';
      else if (w === undefined) hidden = firstWeek !== undefined && weeksBetween(firstWeek, entry.weekLabel) >= maximum ? 'beyond-maximum' : 'outside-horizon';
      else if (bucket!.proposedMps.startsWith('-')) issue = 'negative-result';
    }
    return {
      ...base, visible, issue, hidden,
      currentMps: bucket ? bucket.mps : null,
      proposedMps: bucket && parse.kind === 'value' ? bucket.proposedMps : null,
    };
  });
  const order = (e: EntryEvaluation) => partIndex.get(e.part.toUpperCase()) ?? Number.MAX_SAFE_INTEGER;
  evaluations.sort((a, b) => order(a) - order(b) || a.part.localeCompare(b.part) || a.weekLabel.localeCompare(b.weekLabel));

  const stagedCount = evaluations.filter(e => e.value !== null).length;
  const issueCount = evaluations.filter(e => e.issue !== null).length;
  const hiddenCount = evaluations.filter(e => e.hidden !== null).length;
  return { parts, entries: evaluations, stagedCount, issueCount, hiddenCount, isValid: stagedCount > 0 && issueCount === 0 };
}

/** Counts and syntax problems derivable without a workbook (used for dirty tracking and save guards). */
export function summarizeEntries(entries: AdjustmentEntries) {
  let staged = 0; let invalid = 0;
  for (const entry of Object.values(entries)) {
    const kind = parseAdjustment(entry.text).kind;
    if (kind === 'value') staged++;
    else if (kind !== 'noop') invalid++;
  }
  return { staged, invalid };
}

export interface AdjustedQuarterSummary extends PartQuarterSummary {
  adjustment: string;
  proposedMps: string;
  adjustedMpsDollars: string | null;
  /** Ending balance at the quarter's last visible week; never a sum. */
  adjustedOnHand: string;
}

function adjustedFlows(part: AdjustedPart, quarter: PlanningQuarter): AdjustedBucket[] {
  const weeks = quarter.weekIndexes.map(i => part.weeks[i]);
  return quarter.includesFalldown ? [part.falldown, ...weeks] : weeks;
}

export function adjustedQuarterSummaries(part: AdjustedPart, quarters: readonly PlanningQuarter[]): AdjustedQuarterSummary[] {
  const base = partQuarterSummaries(part as unknown as PlanningPart, quarters);
  return base.map((summary, i) => {
    const quarter = quarters[i];
    const buckets = adjustedFlows(part, quarter);
    const last = part.weeks[quarter.weekIndexes[quarter.weekIndexes.length - 1]];
    return {
      ...summary,
      adjustment: sumDecimals(buckets.map(b => b.adjustment)),
      proposedMps: sumDecimals(buckets.map(b => b.proposedMps)),
      adjustedMpsDollars: part.isPriced && buckets.every(b => b.adjustedMpsDollars !== null)
        ? sumDecimals(buckets.map(b => b.adjustedMpsDollars as string)) : null,
      adjustedOnHand: last.adjustedOnHand,
    };
  });
}

export interface AdjustedWorkspaceFlowTotals extends WorkspaceFlowTotals {
  adjustment: string;
  proposedMps: string;
  /** Null when any retained part is unpriced (same owner-confirmed rule as workspace MPS $). */
  adjustedMpsDollars: string | null;
}

function adjustedWorkspaceFlows(parts: readonly AdjustedPart[], pick: (part: AdjustedPart) => AdjustedBucket[]): AdjustedWorkspaceFlowTotals {
  const all = parts.flatMap(pick);
  const anyUnpriced = parts.some(p => !p.isPriced);
  const dollars = (get: (b: AdjustedBucket) => string | null) =>
    anyUnpriced || all.some(b => get(b) === null) ? null : sumDecimals(all.map(b => get(b) as string));
  return {
    salesOrderQuantity: sumDecimals(all.map(b => b.salesOrderQuantity)),
    salesOrderDollars: sumDecimals(all.map(b => b.salesOrderDollars)),
    mps: sumDecimals(all.map(b => b.mps)),
    mpsDollars: dollars(b => b.mpsDollars),
    adjustment: sumDecimals(all.map(b => b.adjustment)),
    proposedMps: sumDecimals(all.map(b => b.proposedMps)),
    adjustedMpsDollars: dollars(b => b.adjustedMpsDollars),
  };
}

/** Workspace flow totals per Falldown, week and quarter. Inventory balances are never summed. */
export function adjustedWorkspaceTotals(parts: readonly AdjustedPart[], weekCount: number, quarters: readonly PlanningQuarter[]) {
  return {
    falldown: adjustedWorkspaceFlows(parts, p => [p.falldown]),
    weeks: Array.from({ length: weekCount }, (_, i) => adjustedWorkspaceFlows(parts, p => [p.weeks[i]])),
    quarters: quarters.map(q => adjustedWorkspaceFlows(parts, p => adjustedFlows(p, q))),
  };
}

export const ISSUE_MESSAGES: Record<EntryIssue, string> = {
  'invalid-number': 'Enter a whole number such as 25 or -25 (no +, decimals, commas, spaces or exponents).',
  'not-whole-number': 'Adjustments must be whole numbers.',
  'negative-result': 'Proposed MPS would be negative. Reduce the decrease or remove the adjustment.',
  'week-passed': 'This week has passed and is now in Falldown, which is not adjustable. Remove the adjustment.',
  'part-out-of-scope': 'This part is not in the loaded MPS for this workspace. Remove the adjustment.',
};

export const HIDDEN_MESSAGES: Record<EntryHidden, string> = {
  'outside-horizon': 'Not shown at the current horizon. Widen the horizon to review it. Its MPS baseline is not checked here; export validates it against the full loaded MPS.',
  'beyond-maximum': 'Beyond the latest week the loaded MPS reaches, so it cannot be shown. Its MPS baseline is not checked here; export validates it against the full loaded MPS.',
};

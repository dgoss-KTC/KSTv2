/**
 * Stage 14.5 Master plan (Planning) redesign: pure presentation helpers. Every value comes from the
 * accepted exact-decimal workbook evaluation (planningAdjustments.ts / planningWorkbookSummary.ts);
 * nothing here changes a calculation, and no value is converted to a JavaScript number.
 */
import { equalDecimal, formatDecimal, isNegativeDecimal, sumDecimals } from '../shared/exactDecimal';
import { exactProduct, type AdjustedBucket, type AdjustedPart, type AdjustedWorkspaceFlowTotals, type EntryEvaluation } from './planningAdjustments';

/* ---------------------------------------------------------------------------------------------- */
/* Formatting                                                                                      */
/* ---------------------------------------------------------------------------------------------- */

export const qty = (value: string) => formatDecimal(value, null);
export const money = (value: string) => `$${formatDecimal(value, 2)}`.replace('$-', '-$');
export const price = (value: string) => `$${formatDecimal(value, 4)}`;
export const isExactZero = (value: string) => equalDecimal(value, '0');
/** Signed display for changes: "+50", "-50" (exact text, grouped). */
export const signedQty = (value: string) => (isNegativeDecimal(value) ? qty(value) : `+${qty(value)}`);
export const signedMoney = (value: string) => (isNegativeDecimal(value) ? money(value) : `+${money(value)}`);

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
/** Abbreviated Monday label, for example "Oct 5", derived from the ISO text itself. */
export function shortWeekLabel(iso: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso);
  return match ? `${MONTHS[Number(match[2]) - 1]} ${Number(match[3])}` : '';
}

/* ---------------------------------------------------------------------------------------------- */
/* Rows and totals                                                                                 */
/* ---------------------------------------------------------------------------------------------- */

/** Selectable metric rows, in the fixed display order. Adjust is added in planning mode only. */
export type RowId = 'so' | 'soDollars' | 'mps' | 'mpsDollars' | 'forecast' | 'onHand';
/**
 * Full row order. `customerForecast` is reserved for the future customer-forecast import (spec §11):
 * it is never rendered in this checkpoint.
 */
export type GridRowId = RowId | 'customerForecast' | 'adjust';
export type RowKey = 'demand' | 'supply' | 'forecast' | 'adjust' | 'onHand';
export const ROW_ORDER: readonly GridRowId[] = ['so', 'soDollars', 'mps', 'mpsDollars', 'forecast', 'customerForecast', 'adjust', 'onHand'];
export const ROW_DEFS: Record<GridRowId, { label: string; key: RowKey }> = {
  so: { label: 'Sales orders', key: 'demand' },
  soDollars: { label: 'SO $', key: 'demand' },
  mps: { label: 'MPS', key: 'supply' },
  mpsDollars: { label: 'MPS $', key: 'supply' },
  forecast: { label: 'Forecast (QAD)', key: 'forecast' },
  customerForecast: { label: 'Customer forecast', key: 'forecast' },
  adjust: { label: 'Adjust', key: 'adjust' },
  onHand: { label: 'Projected on hand', key: 'onHand' },
};
export const SELECTABLE_ROWS: readonly RowId[] = ['so', 'soDollars', 'mps', 'mpsDollars', 'forecast', 'onHand'];
export const DEFAULT_ROWS: readonly RowId[] = ['so', 'mps', 'onHand'];

/** Workspace total rows. The two dollar totals are pinned by default (owner decision 2026-10-06). */
export type TotalId = 'soDollars' | 'mpsDollars' | 'so' | 'mps' | 'adjust';
export const TOTAL_ORDER: readonly TotalId[] = ['so', 'soDollars', 'mps', 'mpsDollars', 'adjust'];
export const TOTAL_DEFS: Record<TotalId, { label: string; key: RowKey; planningOnly?: boolean }> = {
  so: { label: 'Total sales orders', key: 'demand' },
  soDollars: { label: 'Total SO $', key: 'demand' },
  mps: { label: 'Total MPS', key: 'supply' },
  mpsDollars: { label: 'Total MPS $', key: 'supply' },
  adjust: { label: 'Total adjust', key: 'adjust', planningOnly: true },
};
export const DEFAULT_TOTALS: readonly TotalId[] = ['soDollars', 'mpsDollars'];

/* ---------------------------------------------------------------------------------------------- */
/* View preferences (per assignment, this computer)                                                */
/* ---------------------------------------------------------------------------------------------- */

export interface PlanningViewPreferences {
  /** Upper-cased part numbers hidden from the grid (never removes staged changes). */
  hiddenModels: string[];
  rows: RowId[];
  totals: TotalId[];
  /** Upper-cased part numbers whose model block is collapsed. */
  collapsed: string[];
  /** Subtle row washes by row type (demand, supply, adjust). Default on; a stored false stays false. */
  tintRows: boolean;
}

export const DEFAULT_VIEW_PREFERENCES: PlanningViewPreferences = {
  hiddenModels: [], rows: [...DEFAULT_ROWS], totals: [...DEFAULT_TOTALS], collapsed: [], tintRows: true,
};

export const viewPreferencesKey = (assignmentId: string) => `kst.planningWorkbook.view.v1.${assignmentId}`;

const stringList = (value: unknown): string[] | null =>
  Array.isArray(value) && value.every(v => typeof v === 'string') ? [...new Set(value as string[])] : null;

/** Parses stored preferences defensively; anything malformed falls back to the default for that field. */
export function parseViewPreferences(raw: string | null): PlanningViewPreferences {
  if (!raw) return { ...DEFAULT_VIEW_PREFERENCES };
  let value: Record<string, unknown>;
  try {
    const parsed: unknown = JSON.parse(raw);
    if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) return { ...DEFAULT_VIEW_PREFERENCES };
    value = parsed as Record<string, unknown>;
  } catch {
    return { ...DEFAULT_VIEW_PREFERENCES };
  }
  const rows = stringList(value.rows)?.filter((r): r is RowId => (SELECTABLE_ROWS as readonly string[]).includes(r));
  const totals = stringList(value.totals)?.filter((t): t is TotalId => (TOTAL_ORDER as readonly string[]).includes(t));
  return {
    hiddenModels: (stringList(value.hiddenModels) ?? []).map(p => p.toUpperCase()),
    rows: rows ?? [...DEFAULT_ROWS],
    totals: totals ?? [...DEFAULT_TOTALS],
    collapsed: (stringList(value.collapsed) ?? []).map(p => p.toUpperCase()),
    // Records saved before this field existed, or with a malformed value, keep the default (on).
    tintRows: typeof value.tintRows === 'boolean' ? value.tintRows : true,
  };
}

/**
 * Reconciles stored model choices with the current scope: parts no longer in the workbook are dropped,
 * so a part that leaves and later re-enters scope reappears visible and expanded.
 */
export function reconcileViewPreferences(prefs: PlanningViewPreferences, parts: readonly { part: string }[]): PlanningViewPreferences {
  const scope = new Set(parts.map(p => p.part.toUpperCase()));
  const hiddenModels = prefs.hiddenModels.filter(p => scope.has(p));
  const collapsed = prefs.collapsed.filter(p => scope.has(p));
  return hiddenModels.length === prefs.hiddenModels.length && collapsed.length === prefs.collapsed.length
    ? prefs : { ...prefs, hiddenModels, collapsed };
}

/* ---------------------------------------------------------------------------------------------- */
/* Model status and filters                                                                        */
/* ---------------------------------------------------------------------------------------------- */

export type PlanningMode = 'report' | 'planning';
export type ModelFilter = 'all' | 'short' | 'changed';

/** Projected on hand for the mode: Est. On Hand in report mode, Adj On Hand in planning mode. */
export const onHandFor = (bucket: AdjustedBucket, mode: PlanningMode) => (mode === 'planning' ? bucket.adjustedOnHand : bucket.estimatedOnHand);

/** Visible weeks (Falldown excluded: it cannot be adjusted) with negative projected on hand. */
export function shortWeekCount(part: AdjustedPart, mode: PlanningMode): number {
  return part.weeks.filter(w => isNegativeDecimal(onHandFor(w, mode))).length;
}

/** Entries with input text for a part (proposals and invalid input; no-ops excluded). */
export function changeCountsByPart(entries: readonly EntryEvaluation[]): Map<string, number> {
  const counts = new Map<string, number>();
  for (const e of entries) {
    if (e.noop) continue;
    const key = e.part.toUpperCase();
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }
  return counts;
}

export function filterModels(parts: readonly AdjustedPart[], hidden: ReadonlySet<string>, filter: ModelFilter,
  mode: PlanningMode, changes: ReadonlyMap<string, number>) {
  const tailored = parts.filter(p => !hidden.has(p.part.toUpperCase()));
  const isShort = (p: AdjustedPart) => shortWeekCount(p, mode) > 0;
  const isChanged = (p: AdjustedPart) => (changes.get(p.part.toUpperCase()) ?? 0) > 0;
  const effective: ModelFilter = filter === 'changed' && mode !== 'planning' ? 'all' : filter;
  const shown = effective === 'short' ? tailored.filter(isShort) : effective === 'changed' ? tailored.filter(isChanged) : tailored;
  return {
    tailored,
    shown,
    counts: { all: tailored.length, short: tailored.filter(isShort).length, changed: tailored.filter(isChanged).length },
  };
}

/* ---------------------------------------------------------------------------------------------- */
/* Cells                                                                                           */
/* ---------------------------------------------------------------------------------------------- */

export interface CellView {
  /** Displayed value ('' for an unchanged exact zero). */
  text: string;
  /** Loaded value struck through beside a changed value (planning mode). */
  was?: string;
  title?: string;
  changed?: boolean;
  negative?: boolean;
  invalid?: boolean;
}

const blankZero = (value: string, format: (v: string) => string) => (isExactZero(value) ? '' : format(value));

function plain(value: string, format: (v: string) => string, title?: string): CellView {
  return { text: blankZero(value, format), title: title ?? value };
}

/**
 * A value changed by staged adjustments: the proposed value always prints (so "0" appears when a change
 * brings it to zero). The loaded value is supplied for the struck-through display; the grid shows it
 * only when it fits beside the complete proposed value in the measured column (see PlanningGrid), and
 * the tooltip always carries it.
 */
function changedCell(loaded: string, proposed: string, format: (v: string) => string, detail?: string): CellView {
  const text = format(proposed);
  const wasText = format(loaded);
  const title = `Proposed ${format(proposed)}; loaded ${format(loaded)}${detail ? `; ${detail}` : ''}`;
  return { text, was: wasText, title, changed: true, negative: isNegativeDecimal(proposed) };
}

const NEGATIVE_ON_HAND = 'Projected on hand is negative (display only; never blocks export)';

export function onHandCell(estimated: string, adjusted: string, mode: PlanningMode, balanceNote?: string): CellView {
  const value = mode === 'planning' ? adjusted : estimated;
  const base: CellView = mode === 'planning' && !equalDecimal(adjusted, estimated)
    ? changedCell(estimated, adjusted, qty, balanceNote)
    : plain(value, qty, balanceNote ? `${value}; ${balanceNote}` : value);
  return isNegativeDecimal(value) ? { ...base, negative: true, title: `${NEGATIVE_ON_HAND}. ${base.title ?? ''}`.trim() } : { ...base, negative: false };
}

/** Weekly / Falldown / quarter cell for a non-input row. `adjustment` is the staged nonzero change for the cell or quarter. */
export function metricCell(row: Exclude<GridRowId, 'adjust' | 'customerForecast'>, source: {
  salesOrderQuantity: string; salesOrderDollars: string; mps: string; mpsDollars: string; proposedMps: string;
  adjustedMpsDollars: string; estimatedOnHand: string; adjustedOnHand: string; adjustment: string; forecast?: string;
}, mode: PlanningMode, kind: 'falldown' | 'week' | 'quarter'): CellView {
  const quarter = kind === 'quarter';
  const changed = mode === 'planning' && kind !== 'falldown' && !isExactZero(source.adjustment) && !equalDecimal(source.mps, source.proposedMps);
  switch (row) {
    case 'so': return plain(source.salesOrderQuantity, qty);
    case 'soDollars': return plain(source.salesOrderDollars, money);
    case 'mps': {
      if (!changed) return plain(source.mps, qty);
      const cell = changedCell(source.mps, source.proposedMps, qty, kind === 'week' ? `change ${signedQty(source.adjustment)}` : undefined);
      return cell.negative ? { ...cell, invalid: true, title: `Proposed MPS cannot be negative. ${cell.title}` } : cell;
    }
    case 'mpsDollars': {
      // A zero price (genuine, or $0 for a part with no list price) leaves dollars unchanged by a quantity change: show them as loaded.
      if (!changed || equalDecimal(source.mpsDollars, source.adjustedMpsDollars)) return plain(source.mpsDollars, money);
      const cell = changedCell(source.mpsDollars, source.adjustedMpsDollars, money);
      return cell.negative ? { ...cell, invalid: true } : cell;
    }
    case 'forecast':
      return quarter ? { text: '', title: 'Forecast has no quarter total' } : { text: blankZero(source.forecast ?? '0', qty), title: 'Forecast currently loaded in QAD (display only)' };
    case 'onHand': return onHandCell(source.estimatedOnHand, source.adjustedOnHand, mode, quarter ? 'balance at the last shown week of the quarter' : undefined);
  }
}

/* ---------------------------------------------------------------------------------------------- */
/* Workspace totals                                                                                */
/* ---------------------------------------------------------------------------------------------- */

/** Explains parts counted at $0 (owner-approved amendment 2026-10-07): they stay in the totals; nothing is blank. */
export const NO_LIST_PRICE_TOTAL_NOTE = (count: number) =>
  `${count === 1 ? '1 part in this workspace has' : `${count} parts in this workspace have`} no eligible QAD list price and count as $0 in workspace MPS $ totals. Maintain list prices in QAD to include them; SO $ totals come from sales-order lines.`;

/** Total cell for the complete workspace (independent of hidden models and filters). Inventory is never summed. */
export function totalCell(total: TotalId, flows: AdjustedWorkspaceFlowTotals, mode: PlanningMode): CellView {
  switch (total) {
    case 'so': return plain(flows.salesOrderQuantity, qty);
    case 'soDollars': return plain(flows.salesOrderDollars, money);
    case 'mps':
      return mode === 'planning' && !equalDecimal(flows.mps, flows.proposedMps) ? changedCell(flows.mps, flows.proposedMps, qty) : plain(flows.mps, qty);
    case 'mpsDollars': {
      return mode === 'planning' && !equalDecimal(flows.mpsDollars, flows.adjustedMpsDollars)
        ? changedCell(flows.mpsDollars, flows.adjustedMpsDollars, money) : plain(flows.mpsDollars, money);
    }
    case 'adjust': return { text: isExactZero(flows.adjustment) ? '' : signedQty(flows.adjustment), title: flows.adjustment, changed: !isExactZero(flows.adjustment) };
  }
}

/* ---------------------------------------------------------------------------------------------- */
/* Net change (Changes panel)                                                                      */
/* ---------------------------------------------------------------------------------------------- */

export interface NetChange {
  units: string;
  /** Sum over valid entries at each part's list price ($0 when the part has no eligible list price). */
  dollars: string;
  /** Valid entries for parts with no eligible list price, which count as $0. */
  noListPriceEntries: number;
  validEntries: number;
}

/** Net change across every valid, nonzero staged entry (hidden ones included; entries with issues excluded). */
export function netChange(entries: readonly EntryEvaluation[], parts: readonly AdjustedPart[]): NetChange {
  const prices = new Map(parts.map(p => [p.part.toUpperCase(), p] as const));
  const valid = entries.filter(e => e.value !== null && e.issue === null);
  const units = sumDecimals(valid.map(e => e.value as string));
  const dollars: string[] = [];
  let noListPrice = 0;
  for (const e of valid) {
    const part = prices.get(e.part.toUpperCase());
    if (part === undefined) continue; // not in the loaded workbook: such an entry carries an issue and is never valid
    if (!part.isPriced) noListPrice++;
    dollars.push(exactProduct(e.value as string, part.selectedPrice));
  }
  return { units, dollars: sumDecimals(dollars), noListPriceEntries: noListPrice, validEntries: valid.length };
}

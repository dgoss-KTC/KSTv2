import { useEffect, useLayoutEffect, useMemo, useRef, type CSSProperties, type PointerEvent as ReactPointerEvent } from 'react';
import { formatPlanningDate, type PlanningColumn, type PlanningWorkbook } from '../../planningWorkbook/planningWorkbookSummary';
import { quarterDetail } from './planningLabels';
import { hoverColumnCss, sizePlanningColumns } from './planningColumnSizing';
import { ISSUE_MESSAGES, type AdjustedBucket, type AdjustedPart, type AdjustedQuarterSummary, type adjustedWorkspaceTotals } from '../../planningWorkbook/planningAdjustments';
import {
  isExactZero, metricCell, onHandCell, onHandFor, price, qty, ROW_DEFS, ROW_ORDER, shortWeekCount, shortWeekLabel, signedQty,
  TOTAL_DEFS, totalCell, NO_LIST_PRICE_TOTAL_NOTE, type CellView, type GridRowId, type PlanningMode, type RowId, type TotalId,
} from '../../planningWorkbook/planningView';

type Totals = ReturnType<typeof adjustedWorkspaceTotals>;
type ColumnKind = 'falldown' | 'week' | 'quarter';

function cellClass(cell: CellView, kind: ColumnKind, current: boolean, extra = '') {
  return ['pwb-cell', `pwb-cell--${kind}`, current && 'pwb-cell--current', cell.changed && 'pwb-cell--changed',
    cell.negative && 'pwb-cell--negative', cell.invalid && 'pwb-cell--invalid', extra]
    .filter(Boolean).join(' ');
}

function GridCell({ cell, kind, current = false }: { cell: CellView; kind: ColumnKind; current?: boolean }) {
  return (
    <td className={cellClass(cell, kind, current)} title={cell.title}>
      {cell.was !== undefined && <s className="pwb-was" aria-hidden="true">{cell.was}</s>}
      <span className="pwb-v">{cell.text}</span>
      {cell.changed && cell.title && <span className="pwb-sr-only">{` (${cell.title})`}</span>}
    </td>
  );
}


export interface PlanningGridProps {
  workbook: PlanningWorkbook;
  /** Models to draw (after View tailoring and the active filter). */
  shown: readonly AdjustedPart[];
  /** Number of models in the complete workspace (totals always cover all of them). */
  modelCount: number;
  columns: readonly PlanningColumn[];
  summaries: ReadonlyMap<string, AdjustedQuarterSummary[]>;
  totals: Totals;
  mode: PlanningMode;
  rows: readonly RowId[];
  totalRows: readonly TotalId[];
  /** Row tint by type (View → Row color). Default on. */
  tint?: boolean;
  collapsed: ReadonlySet<string>;
  changeCounts: ReadonlyMap<string, number>;
  onToggleCollapse: (part: string) => void;
  onSetAllCollapsed: (collapsed: boolean) => void;
  onAdjust?: (part: string, weekLabel: string, text: string) => void;
  onEndEdit?: () => void;
  emptyMessage?: { text: string; actionLabel: string; onAction: () => void } | null;
}

function AdjustInput({ part, bucket, current, onAdjust, onEndEdit }: {
  part: string; bucket: AdjustedBucket; current: boolean;
  onAdjust?: (part: string, weekLabel: string, text: string) => void; onEndEdit?: () => void;
}) {
  const weekLabel = bucket.weekLabel as string;
  const issue = bucket.inputIssue;
  const classes = ['pwb-cell', 'pwb-cell--week', 'pwb-cell--input', current && 'pwb-cell--current', issue && 'pwb-cell--invalid', bucket.input !== '' && 'pwb-cell--has-input']
    .filter(Boolean).join(' ');
  return (
    <td className={classes}>
      <input
        className="pwb-adjust"
        type="text"
        inputMode="numeric"
        autoComplete="off"
        spellCheck={false}
        value={bucket.input}
        readOnly={!onAdjust}
        aria-label={`Adjustment for ${part}, week of ${formatPlanningDate(weekLabel)}`}
        aria-invalid={issue ? true : undefined}
        title={issue ? ISSUE_MESSAGES[issue] : 'Signed whole number, for example 25 or -25. Blank means no change.'}
        onChange={e => onAdjust?.(part, weekLabel, e.target.value)}
        onBlur={() => onEndEdit?.()}
      />
    </td>
  );
}

function ModelBlock({ part, props, summaries }: { part: AdjustedPart; props: PlanningGridProps; summaries: AdjustedQuarterSummary[] }) {
  const { columns, mode, rows, collapsed, changeCounts } = props;
  const key = part.part.toUpperCase();
  const isCollapsed = collapsed.has(key);
  const short = shortWeekCount(part, mode);
  const changes = changeCounts.get(key) ?? 0;
  const gridRows = ROW_ORDER.filter((id): id is GridRowId => id === 'adjust' ? mode === 'planning' : id !== 'customerForecast' && rows.includes(id as RowId));

  const health = (cellKey: string, value: string, kind: ColumnKind, current: boolean, balance?: boolean) => {
    const cell = onHandCell(value, value, 'report', balance ? 'balance at the last shown week of the quarter' : undefined);
    const negative = !!cell.negative && kind !== 'quarter';
    return (
      <td key={cellKey} className={['pwb-cell', `pwb-cell--${kind}`, 'pwb-health', current && 'pwb-cell--current', negative && 'pwb-health--negative', isCollapsed && cell.negative && 'pwb-cell--negative'].filter(Boolean).join(' ')}
        title={isCollapsed || negative ? cell.title : undefined}>
        {isCollapsed && <span className="pwb-v">{cell.text}</span>}
      </td>
    );
  };

  return (
    <tbody className={`pwb-model${isCollapsed ? ' pwb-model--collapsed' : ''}`} aria-label={`Part ${part.part}`}>
      <tr className="pwb-model__header">
        <th scope="rowgroup" className="pwb-model__id">
          <button type="button" className="pwb-chevron" aria-expanded={!isCollapsed}
            aria-label={`${isCollapsed ? 'Expand' : 'Collapse'} ${part.part}`} onClick={() => props.onToggleCollapse(part.part)}>
            <span aria-hidden="true">{'\u25be'}</span>
          </button>
          <span className="pwb-model__text">
            <span className="pwb-model__line">
              <span className="pwb-model__part">{part.part}</span>
              {part.description && <span className="pwb-model__desc" title={part.description}>{part.description}</span>}
            </span>
            <span className="pwb-model__meta">
              On hand <strong>{qty(part.netQuantityOnHand)}</strong>{' \u00b7 '}
              <span title={part.isPriced ? `Selected price ${part.selectedPrice}` : 'No eligible QAD list price was found; $0 is used for MPS $'}>{price(part.selectedPrice)}</span>
              {!part.isPriced && <strong className="pwb-badge pwb-badge--unpriced" title="No eligible QAD list price was found; $0 is used for MPS $">No list price</strong>}
              {short > 0 && <span className="pwb-badge pwb-badge--short">{short} {short === 1 ? 'wk' : 'wks'} short</span>}
              {mode === 'planning' && changes > 0 && <span className="pwb-badge pwb-badge--changes">{changes} {changes === 1 ? 'change' : 'changes'}</span>}
            </span>
          </span>
        </th>
        {health('f', onHandFor(part.falldown, mode), 'falldown', false)}
        {columns.map(column => column.kind === 'week'
          ? health(`w${column.index}`, onHandFor(part.weeks[column.index], mode), 'week', column.index === 0)
          : health(`q${column.position}`, mode === 'planning' ? summaries[column.position].adjustedOnHand : summaries[column.position].estimatedOnHand, 'quarter', false, true))}
      </tr>
      {!isCollapsed && gridRows.map(id => (
        <tr key={id} className={`pwb-row pwb-row--${id}`}>
          <th scope="row" className="pwb-label">
            <span className={`pwb-key pwb-key--${ROW_DEFS[id].key}`} aria-hidden="true" />
            {ROW_DEFS[id].label}
          </th>
          {id === 'adjust'
            ? <>
              <GridCell cell={{ text: '', title: 'Falldown is not adjustable' }} kind="falldown" />
              {columns.map(column => column.kind === 'week'
                ? <AdjustInput key={`w${column.index}`} part={part.part} bucket={part.weeks[column.index]} current={column.index === 0} onAdjust={props.onAdjust} onEndEdit={props.onEndEdit} />
                : <GridCell key={`q${column.position}`} kind="quarter"
                  cell={isExactZero(summaries[column.position].adjustment) ? { text: '', title: 'No net change in this quarter' }
                    : { text: signedQty(summaries[column.position].adjustment), title: `Net change ${summaries[column.position].adjustment}`, changed: true }} />)}
            </>
            : <>
              <GridCell cell={metricCell(id as Exclude<GridRowId, 'adjust' | 'customerForecast'>, part.falldown, mode, 'falldown')} kind="falldown" />
              {columns.map(column => column.kind === 'week'
                ? <GridCell key={`w${column.index}`} kind="week" current={column.index === 0}
                  cell={metricCell(id as Exclude<GridRowId, 'adjust' | 'customerForecast'>, part.weeks[column.index], mode, 'week')} />
                : <GridCell key={`q${column.position}`} kind="quarter"
                  cell={metricCell(id as Exclude<GridRowId, 'adjust' | 'customerForecast'>, summaries[column.position], mode, 'quarter')} />)}
            </>}
        </tr>
      ))}
    </tbody>
  );
}

/**
 * Hover crosshair: one delegated handler records the hovered cell's column index on the scroll
 * container (no React state, so pointer movement never re-renders the grid). The row is highlighted by
 * CSS :hover. Row labels (index 0) highlight their row only.
 */
function trackHoverColumn(event: ReactPointerEvent<HTMLDivElement>) {
  const container = event.currentTarget;
  const target = event.target instanceof Element ? event.target : null;
  const cell = target?.closest('th, td') as HTMLTableCellElement | null;
  const row = cell?.parentElement;
  const index = cell && row && container.contains(cell) && row.children.length > 2 ? cell.cellIndex : -1;
  const next = index >= 1 ? String(index) : null;
  if ((container.getAttribute('data-hover-col') ?? null) === next) return;
  if (next === null) container.removeAttribute('data-hover-col'); else container.setAttribute('data-hover-col', next);
}
const clearHoverColumn = (event: ReactPointerEvent<HTMLDivElement>) => event.currentTarget.removeAttribute('data-hover-col');

/**
 * Compact Master plan grid: one sticky column header, model blocks with sticky headers and a pinned
 * model column, and workspace totals pinned to the bottom. Quarter grouping comes from
 * planningQuarters/planningColumns; every value comes from the exact-decimal evaluation.
 */
export function PlanningGrid(props: PlanningGridProps) {
  const { workbook, shown, columns, totals, mode, totalRows, collapsed, tint = true } = props;
  const anyOpen = shown.some(p => !collapsed.has(p.part.toUpperCase()));
  const noListPrice = workbook.parts.filter(p => !p.isPriced).length;
  const visibleTotals = totalRows.filter(t => !TOTAL_DEFS[t].planningOnly || mode === 'planning');
  const columnCount = 2 + columns.length;
  const hoverCss = useMemo(() => hoverColumnCss(columnCount), [columnCount]);

  // Column widths follow the rendered content: measured after every grid commit (data, adjustments,
  // mode, horizon, row and total choices) and when the density attribute or the loaded fonts change.
  const tableRef = useRef<HTMLTableElement>(null);
  const measureRef = useRef<HTMLSpanElement>(null);
  useLayoutEffect(() => { sizePlanningColumns(tableRef.current, measureRef.current); });
  useEffect(() => {
    const resize = () => sizePlanningColumns(tableRef.current, measureRef.current);
    const host = tableRef.current?.closest('[data-density]') ?? document.documentElement;
    const observer = typeof MutationObserver === 'function' ? new MutationObserver(resize) : null;
    observer?.observe(host, { attributes: true, attributeFilter: ['data-density', 'data-theme'] });
    const fonts = typeof document !== 'undefined' ? document.fonts : undefined;
    fonts?.addEventListener?.('loadingdone', resize);
    void fonts?.ready?.then(resize);
    return () => {
      observer?.disconnect();
      fonts?.removeEventListener?.('loadingdone', resize);
    };
  }, []);

  return (
    <div className="pwb-grid-scroll" role="region" aria-label="Planning workbook grid" tabIndex={0}
      onPointerOver={trackHoverColumn} onPointerLeave={clearHoverColumn}>
      <style>{hoverCss}</style>
      <span ref={measureRef} className="pwb-measure" aria-hidden="true" />
      <table ref={tableRef} className={`pwb-grid${tint ? ' pwb-grid--tint' : ''}`} data-testid="planning-grid-table">
        <thead>
          <tr>
            <th scope="col" className="pwb-corner">
              <span>Model</span>
              {shown.length > 0 && (
                <button type="button" className="pwb-link-btn" onClick={() => props.onSetAllCollapsed(anyOpen)}>
                  {anyOpen ? 'Collapse all' : 'Expand all'}
                </button>
              )}
            </th>
            <th scope="col" className="pwb-head pwb-head--falldown" title="Falldown: everything due before this week. Not adjustable.">
              <span className="pwb-head__label">Falldown</span>
              <span className="pwb-head__sub">past due</span>
            </th>
            {columns.map(column => {
              if (column.kind === 'week') {
                const iso = workbook.weekLabels[column.index];
                return (
                  <th key={`w${column.index}`} scope="col" data-week={iso} title={`Week of ${formatPlanningDate(iso)}`}
                    className={`pwb-head pwb-head--week${column.index === 0 ? ' pwb-head--current' : ''}`}>
                    <span className="pwb-head__label">{shortWeekLabel(iso)}</span>
                    {column.index === 0 && <span className="pwb-head__sub">this week</span>}
                    <span className="pwb-sr-only">{`, week of ${formatPlanningDate(iso)}`}</span>
                  </th>
                );
              }
              const q = column.quarter;
              return (
                <th key={`q${column.position}`} scope="col" data-quarter={q.key} title={quarterDetail(q)} className="pwb-head pwb-head--quarter">
                  <span className="pwb-head__label">Q{q.fiscalQuarter} total</span>
                  <span className="pwb-head__sub">FY{String(q.fiscalYear).slice(-2)} {'\u00b7'} {q.weekIndexes.length} {q.weekIndexes.length === 1 ? 'wk' : 'wks'}</span>
                  <span className="pwb-sr-only">{`. ${quarterDetail(q)}`}</span>
                </th>
              );
            })}
          </tr>
        </thead>
        {shown.map(part => (
          <ModelBlock key={part.part} part={part} props={props} summaries={props.summaries.get(part.part.toUpperCase()) ?? []} />
        ))}
        {shown.length === 0 && props.emptyMessage && (
          <tbody>
            <tr>
              <td className="pwb-grid__empty" colSpan={columnCount}>
                {props.emptyMessage.text}{' '}
                <button type="button" className="pwb-btn" onClick={props.emptyMessage.onAction}>{props.emptyMessage.actionLabel}</button>
              </td>
            </tr>
          </tbody>
        )}
        {visibleTotals.length > 0 && (
          <tfoot aria-label="Workspace totals">
            {visibleTotals.map((id, i) => {
              const def = TOTAL_DEFS[id];
              const note = NO_LIST_PRICE_TOTAL_NOTE(noListPrice);
              return (
                <tr key={id} className={`pwb-total pwb-total--${id}`} style={{ '--pwb-foot-i': visibleTotals.length - 1 - i } as CSSProperties}>
                  <th scope="row" className="pwb-label pwb-total__label">
                    <span className={`pwb-key pwb-key--${def.key}`} aria-hidden="true" />
                    {def.label}
                    <span className="pwb-total__scope" title="Totals always cover every model in the workspace, including hidden or filtered models.">
                      all {props.modelCount} {props.modelCount === 1 ? 'model' : 'models'}
                    </span>
                    {(id === 'mpsDollars') && noListPrice > 0 && (
                      <span className="pwb-total__warn" data-testid="mps-dollar-note" title={note}>
                        {noListPrice} at $0<span className="pwb-sr-only">{`. ${note}`}</span>
                      </span>
                    )}
                  </th>
                  <GridCell kind="falldown" cell={totalCell(id, totals.falldown, mode)} />
                  {columns.map(column => column.kind === 'week'
                    ? <GridCell key={`w${column.index}`} kind="week" current={column.index === 0} cell={totalCell(id, totals.weeks[column.index], mode)} />
                    : <GridCell key={`q${column.position}`} kind="quarter" cell={totalCell(id, totals.quarters[column.position], mode)} />)}
                </tr>
              );
            })}
          </tfoot>
        )}
      </table>
    </div>
  );
}

import { memo, useCallback, useId, useMemo, useRef, useState, type CSSProperties } from 'react';
import { createPortal } from 'react-dom';
import {
  DEFAULT_LONG_TERM_SHORTAGE_POPULATION_OPTIONS,
  exportLongTermShortages,
  type LongTermShortageScreenRow as LongTermShortageRow,
} from '../api/longTermShortagesApi';
import { useLongTermShortages } from '../hooks/useLongTermShortages';
import { useLongTermShortagePurchasing } from '../hooks/useLongTermShortagePurchasing';
import { useLongTermShortageProjectionDetail } from '../hooks/useLongTermShortageProjectionDetail';
import {
  EMPTY_LONG_TERM_SHORTAGES_FILTERS,
  filterLongTermShortages,
  formatLongTermDate,
} from '../longTermShortages/longTermShortagesPresentation';
import { saveLongTermShortagesWorkbook } from '../longTermShortages/saveLongTermShortagesWorkbook';
import { useComponentDetail } from '../hooks/useComponentDetail';
import { useApprovedVendors } from '../hooks/useApprovedVendors';
import './LongTermShortagesPanel.css';
import { ComponentInfoModal } from './ComponentInfoModal';

export function LongTermShortagesPanel({ assignmentId, snapshotId }: { assignmentId: string; snapshotId: string | null }) {
  const [populationOptions, setPopulationOptions] = useState(DEFAULT_LONG_TERM_SHORTAGE_POPULATION_OPTIONS);
  const { data, loadedOptions, isLoading, isRefreshing, error, retry } = useLongTermShortages(assignmentId, snapshotId, populationOptions);
  const [filters, setFilters] = useState(EMPTY_LONG_TERM_SHORTAGES_FILTERS);
  const [selectedPart, setSelectedPart] = useState<string | null>(null);
  const [isExporting, setIsExporting] = useState(false);
  const [exportError, setExportError] = useState<string | null>(null);
  const [exportSuccess, setExportSuccess] = useState<string | null>(null);
  const origin = useRef<HTMLButtonElement>(null);
  const component = useComponentDetail(assignmentId, selectedPart);
  const vendors = useApprovedVendors(assignmentId, selectedPart);
  const purchasing = useLongTermShortagePurchasing(assignmentId, data?.snapshotId ?? null, selectedPart, loadedOptions);
  const projection = useLongTermShortageProjectionDetail(assignmentId, data?.snapshotId ?? null, selectedPart, loadedOptions);
  const projected = useMemo(() => data?.components.map(row => ({ ...row,
    ...(populationOptions.includeUnconfirmed ? row.all : row.confirmed) })), [data, populationOptions.includeUnconfirmed]);
  const rows = useMemo(() => filterLongTermShortages(projected ?? [], filters), [projected, filters]);
  const selectPart = useCallback((part: string, button: HTMLButtonElement) => {
    origin.current = button;
    setSelectedPart(part);
  }, []);

  if (!snapshotId) return <div className="long-term-shortages__state">Load the MPS dashboard before viewing Workspace Shortages.</div>;
  if (isLoading) return <div className="long-term-shortages__state">Loading Workspace Shortages...</div>;
  if (error && !data) return <div className="long-term-shortages__state long-term-shortages__state--error"><p role="alert">{error.detail}</p><button type="button" onClick={retry}>Retry Workspace Shortages</button></div>;
  if (!data) return null;
  const replacing = isRefreshing;
  const selectedRow = projected?.find((row) => row.componentPart === selectedPart) ?? null;

  return <section className="long-term-shortages" aria-labelledby="long-term-shortages-heading">
    <header className="long-term-shortages__toolbar">
      <h3 id="long-term-shortages-heading">Workspace Shortages</h3>
      <div className="long-term-shortages__filters">
        <label>Comp <input type="search" value={filters.component} onChange={(event) => setFilters({ ...filters, component: event.target.value })} /></label>
        <label>Buyer / Planner <input type="search" value={filters.planner} onChange={(event) => setFilters({ ...filters, planner: event.target.value })} /></label>
        <label><input type="checkbox" checked={filters.kssOnly} onChange={(event) => setFilters({ ...filters, kssOnly: event.target.checked })} /> KSS</label>
        <label><input type="checkbox" checked={filters.statusOnly} onChange={(event) => setFilters({ ...filters, statusOnly: event.target.checked })} /> Status</label>
        <label><input type="checkbox" checked={filters.showAll} onChange={(event) => setFilters({ ...filters, showAll: event.target.checked })} /> Show All</label>
        <label>Sort <select value={filters.sort} onChange={(event) => setFilters({ ...filters, sort: event.target.value as typeof filters.sort })}><option value="mostUrgent">Most urgent</option><option value="firstShortage">First shortage</option><option value="deepestShortage">Deepest shortage</option><option value="recoveryDate">Recovery date</option><option value="buyerPlanner">Buyer/planner</option><option value="component">Component number</option></select></label>
        <label><input type="checkbox" checked={populationOptions.includeManufacturedParts} onChange={(event) => setPopulationOptions({ ...populationOptions, includeManufacturedParts: event.target.checked })} /> Include Manufactured Parts</label>
        <label><input type="checkbox" checked={populationOptions.includeUnconfirmed} onChange={(event) => setPopulationOptions({ ...populationOptions, includeUnconfirmed: event.target.checked })} /> Include Unconfirmed Receipts</label>
        <label>Horizon <select value={populationOptions.horizonWeeks} onChange={(event) => setPopulationOptions({ ...populationOptions, horizonWeeks: Number(event.target.value) as 13 | 26 | 52 | 72 })}>{[13, 26, 52, 72].map((weeks) => <option key={weeks} value={weeks}>{weeks} weeks</option>)}</select></label>
      </div>
      <button type="button" disabled={rows.length === 0 || isExporting || replacing || !!error} onClick={async () => {
        setExportError(null);
        setExportSuccess(null);
        setIsExporting(true);
        try {
          const workbook = await exportLongTermShortages(assignmentId, data.snapshotId, rows.map((row) => row.componentPart), { ...populationOptions, showAll: filters.showAll });
          const result = await saveLongTermShortagesWorkbook(workbook.blob, workbook.fileName ?? `Workspace-Shortages-${data.refreshDate}.xlsx`);
          if (result.kind === 'saved') setExportSuccess(`Saved ${result.fileName}.`);
        } catch {
          setExportError('The workbook could not be prepared. Please retry.');
        } finally {
          setIsExporting(false);
        }
      }}>{isExporting ? 'Preparing workbook...' : 'Export displayed rows'}</button>
    </header>
    {replacing && <p className="long-term-shortages__refreshing" role="status">Refreshing…</p>}
    {error && <p className="long-term-shortages__state--error" role="alert">{error.detail} <button type="button" onClick={retry}>Retry</button></p>}
    {data.isStale && <p className="long-term-shortages__stale" role="alert">Workspace Shortages is stale and cannot be classified as current. {data.warning ?? 'Showing retained results.'} Refresh date: {data.refreshDate}.</p>}
    {exportError && <p className="long-term-shortages__state long-term-shortages__state--error" role="alert">{exportError}</p>}
    {exportSuccess && <p className="long-term-shortages__export-success" role="status">{exportSuccess}</p>}
     {rows.length === 0 ? <div className="long-term-shortages__state">{filters.showAll ? 'No components match the current filters.' : 'No components reach a shortage. Select Show All to view healthy components.'}</div> :
       <div className="long-term-shortages__grid" data-testid="workspace-shortages-grid" tabIndex={0} role="region" aria-label={`Workspace Shortages scrollable grid. Scroll horizontally to view ${data.weeks.length} Sunday through Saturday weeks.`}>
         <table className="long-term-shortages__matrix" aria-label="Workspace Shortages weekly balances" style={{ '--week-count': data.weeks.length } as CSSProperties}>
           <colgroup>
             {['comp', 'qad-status', 'description', 'kss', 'planner', 'on-hand', 'severity'].map((column) => <col key={column} className={`long-term-shortages__column--${column}`} />)}
             {data.weeks.map((week) => <col key={week.weekNumber} className="long-term-shortages__column--week" />)}
           </colgroup>
           <thead><tr><th scope="col">Comp</th><th scope="col" aria-label="QAD Status"><span aria-hidden="true">QAD<br />Status</span></th><th scope="col">Description</th><th scope="col">KSS</th><th scope="col">Buyer / Planner</th><th scope="col" className="long-term-shortages__num">On Hand</th><th scope="col">Severity</th>{data.weeks.map((week) => <th key={week.weekNumber} scope="col" title={week.weekStart}>Week {week.weekNumber}<br />{formatLongTermDate(week.labelDate)}</th>)}</tr></thead>
           <tbody>{rows.map((row) => <ShortageMatrixRow key={row.componentPart} row={row} onSelect={selectPart} />)}</tbody>
         </table>
       </div>}
    {selectedRow && <ComponentInfoModal key={selectedRow.componentPart} context="shortages" shortage={selectedRow} componentPart={selectedRow.componentPart}
      asOfDate={data.refreshDate}
      projectionDetail={projection.data} isProjectionLoading={projection.isLoading} projectionError={projection.error} onRetryProjection={projection.retry}
      purchasing={purchasing.data} isPurchasingLoading={purchasing.isLoading} purchasingError={purchasing.error} onRetryPurchasing={purchasing.retry}
      detail={component.detail} isLoading={component.isLoading} error={component.error} onRetry={component.retry}
      onClose={() => { setSelectedPart(null); origin.current?.focus(); }} approvedVendors={vendors.rows}
      isApprovedVendorsLoading={vendors.isLoading} approvedVendorsError={vendors.error}
      onExpandApprovedVendors={vendors.activate} onRetryApprovedVendors={vendors.retry} />}
  </section>;
}

const ShortageMatrixRow = memo(function ShortageMatrixRow({ row, onSelect }: { row: LongTermShortageRow; onSelect: (part: string, button: HTMLButtonElement) => void }) {
  const currentSeverity = row.weeklySeverity[0];
  const severityClass = currentSeverity === 'CriticalShort' ? ' long-term-shortages__comp--critical' : currentSeverity === 'SafetyStockShort' ? ' long-term-shortages__comp--safety' : '';
  const rowSeverity = row.severity;
  const severityLabel = rowSeverity === 'SafetyStockUnavailable' ? 'SS Unknown' : rowSeverity;
  const explanation = [row.safetyStock === null && rowSeverity === 'CriticalShort' ? 'Safety stock unknown.' : null,
    row.dataQualityWarning]
    .filter(Boolean).join(' ');
  return <tr aria-label={`${row.componentPart}: ${rowSeverity}`}><td className={`long-term-shortages__comp${severityClass}`}><button type="button" onClick={(event) => onSelect(row.componentPart, event.currentTarget)}>{row.componentPart}</button><span className="long-term-shortages__sr-only">, {rowSeverity}</span></td><td>{row.qadStatus ?? ''}</td><td title={row.description ?? undefined}>{row.description ?? ''}</td><td>{row.isKss ? 'KSS' : ''}</td><td title={row.planner ?? undefined}>{row.planner ?? ''}</td><td className="long-term-shortages__num">{row.openingDisplay}</td><td><SeverityLabel label={severityLabel} explanation={explanation} /></td>{row.ending.map((_, index) => <BucketCell key={index} row={row} index={index} />)}</tr>;
});

function SeverityLabel({ label, explanation }: { label: string; explanation: string }) {
  const tooltipId = useId();
  const [position, setPosition] = useState<{ top: number; left: number } | null>(null);
  const show = (element: HTMLElement) => {
    const bounds = element.getBoundingClientRect();
    setPosition({ top: Math.min(bounds.bottom + 4, window.innerHeight - 80), left: Math.max(8, Math.min(bounds.left, window.innerWidth - 328)) });
  };
  return <>
    <span className="long-term-shortages__severity-label" tabIndex={explanation ? 0 : undefined}
      aria-label={explanation ? `${label}: ${explanation}` : undefined}
      aria-describedby={explanation ? tooltipId : undefined}
      onMouseEnter={(event) => { if (explanation) show(event.currentTarget); }} onMouseLeave={() => setPosition(null)}
      onFocus={(event) => { if (explanation) show(event.currentTarget); }} onBlur={() => setPosition(null)}>{label}</span>
    {explanation && position && createPortal(<span id={tooltipId} role="tooltip" className="long-term-shortages__severity-tooltip" style={position}>{explanation}</span>, document.body)}
  </>;
}

function BucketCell({ row, index }: { row: LongTermShortageRow; index: number }) {
  const raw = String(row.ending[index]);
  const negative = raw.startsWith('-') && !/^-0(?:\.0*)?$/.test(raw);
  return <td className={`long-term-shortages__num${negative ? ' long-term-shortages__balance--negative' : ''}`} aria-label={`${row.componentPart}, Week ${index + 1}: ${row.weeklySeverity[index]}, raw official ending ${raw}`}>{row.endingDisplay[index]}</td>;
}

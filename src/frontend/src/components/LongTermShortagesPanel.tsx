import { memo, useCallback, useId, useMemo, useRef, useState, type CSSProperties } from 'react';
import { createPortal } from 'react-dom';
import {
  DEFAULT_LONG_TERM_SHORTAGE_POPULATION_OPTIONS,
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
import { workspaceShortagesExport, type WorkspaceShortagesExportRequest } from '../longTermShortages/workspaceShortagesExport';
import '../shared/shortageSeverity.css';
import { AnalyticalExcelExport } from './AnalyticalExcelExport';
import { useLocalBusinessDate } from '../hooks/useLocalBusinessDate';
import { localIsoDate } from '../shared/localDate';
import { useComponentDetail } from '../hooks/useComponentDetail';
import { useApprovedVendors } from '../hooks/useApprovedVendors';
import './LongTermShortagesPanel.css';
import { ComponentInfoModal } from './ComponentInfoModal';

export function LongTermShortagesPanel({ assignmentId, snapshotId, site = null }: { assignmentId: string; snapshotId: string | null; site?: string | null }) {
  const [populationOptions, setPopulationOptions] = useState(DEFAULT_LONG_TERM_SHORTAGE_POPULATION_OPTIONS);
  const { data, loadedOptions, isLoading, isRefreshing, error, retry } = useLongTermShortages(assignmentId, snapshotId, populationOptions);
  const [filters, setFilters] = useState(EMPTY_LONG_TERM_SHORTAGES_FILTERS);
  const [selectedPart, setSelectedPart] = useState<string | null>(null);
  const businessDate = useLocalBusinessDate();
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

  if (!snapshotId) return <section className="long-term-shortages"><div className="long-term-shortages__state">Load the MPS dashboard before viewing Workspace Shortages.</div></section>;
  if (isLoading) return <section className="long-term-shortages"><div className="long-term-shortages__state">Loading Workspace Shortages...</div></section>;
  if (error && !data) return <section className="long-term-shortages"><div className="long-term-shortages__state long-term-shortages__state--error"><p role="alert">{error.detail}</p><button className="long-term-shortages__retry" type="button" onClick={retry}>Retry Workspace Shortages</button></div></section>;
  if (!data) return null;
  const replacing = isRefreshing;
  const selectedRow = projected?.find((row) => row.componentPart === selectedPart) ?? null;
  // Block immediately, including the render before the hook schedules its acquisition effect.
  const scopePending = loadedOptions.includeManufacturedParts !== populationOptions.includeManufacturedParts ||
    loadedOptions.includePhantoms !== populationOptions.includePhantoms || loadedOptions.horizonWeeks !== populationOptions.horizonWeeks;
  const selection = { ...loadedOptions, snapshotId: data.snapshotId, componentParts: rows.map(row => row.componentPart),
    includeUnconfirmed: populationOptions.includeUnconfirmed, showAll: filters.showAll };
  const exportRequest: WorkspaceShortagesExportRequest = { workspaceId: assignmentId, mpsSnapshotId: data.snapshotId,
    site: site ?? '', businessDate, acquiredAtUtc: data.acquiredAtUtc, selection };
  const exportReason = replacing || scopePending ? 'A replacement projection is pending. Export is available when its rows are loaded.'
    : error ? 'Projection refresh failed. Retry Workspace Shortages before exporting.'
    : !site ? 'Workspace site is unavailable. Reopen the workspace before exporting.'
    : rows.length === 0 ? 'No displayed components to export.' : undefined;
  const alert = [error?.detail, data.isStale ? `Workspace Shortages is stale and cannot be classified as current. ${data.warning ?? 'Showing retained results.'} Refresh date: ${data.refreshDate}.` : null].filter(Boolean).join(' ');

  return <section className="long-term-shortages" aria-labelledby="long-term-shortages-heading">
    <header className="long-term-shortages__toolbar">
      <h3 id="long-term-shortages-heading">Workspace Shortages</h3>
      <span className="long-term-shortages__caption">Showing {rows.length} of {projected?.length ?? 0} components</span>
      <span className="long-term-shortages__caption">Refreshed {data.refreshDate} · {data.weeks.length} weeks</span>
      {replacing && <span className="long-term-shortages__refreshing" role="status">Refreshing…</span>}
      <div className="long-term-shortages__legend" aria-label="Grid colour legend">
        <span><i className="long-term-shortages__swatch--negative" />Negative weekly ending balance</span>
        <span><i className="long-term-shortages__swatch--negative" />Part tint: Week 1 critical short</span>
        <span><i className="long-term-shortages__swatch--safety" />Part tint: Week 1 below safety stock</span>
      </div>
      <AnalyticalExcelExport label="Workspace Shortages" clientFactory={workspaceShortagesExport}
        contextKey={JSON.stringify([exportRequest, filters, populationOptions, data.isStale, data.warning])}
        request={() => {
          if (exportReason) throw new Error(exportReason);
          if (localIsoDate(new Date()) !== businessDate) throw new Error('Business date changed. Reload the view before exporting.');
          return exportRequest;
        }} disabledReason={exportReason}
        scopeLabel={`All ${rows.length} displayed components in current sort order · ${loadedOptions.horizonWeeks} weeks · ${populationOptions.includeUnconfirmed ? 'all receipts' : 'confirmed receipts'} · loaded projection only`} />
    </header>
    <div className="long-term-shortages__controls">
      <div className="long-term-shortages__filters" role="group" aria-label="Loaded-data and view controls">
        <span className="long-term-shortages__group-label">Loaded-data / view</span>
        <label>Comp <input type="search" value={filters.component} onChange={(event) => setFilters({ ...filters, component: event.target.value })} /></label>
        <label>Buyer / Planner <input type="search" value={filters.planner} onChange={(event) => setFilters({ ...filters, planner: event.target.value })} /></label>
        <label>Sort <select value={filters.sort} onChange={(event) => setFilters({ ...filters, sort: event.target.value as typeof filters.sort })}><option value="mostUrgent">Most urgent</option><option value="firstShortage">First shortage</option><option value="deepestShortage">Deepest shortage</option><option value="recoveryDate">Recovery date</option><option value="buyerPlanner">Buyer/planner</option><option value="component">Component number</option></select></label>
        <label><input type="checkbox" checked={filters.includeKss} onChange={(event) => setFilters({ ...filters, includeKss: event.target.checked })} /> Include KSS</label>
        <label><input type="checkbox" checked={filters.statusOnly} onChange={(event) => setFilters({ ...filters, statusOnly: event.target.checked })} /> Status</label>
        <label><input type="checkbox" checked={filters.showAll} onChange={(event) => setFilters({ ...filters, showAll: event.target.checked })} /> Show All</label>
        <label><input type="checkbox" checked={populationOptions.includeUnconfirmed} onChange={(event) => setPopulationOptions({ ...populationOptions, includeUnconfirmed: event.target.checked })} /> Include Unconfirmed Receipts</label>
      </div>
      <div className="long-term-shortages__filters" role="group" aria-label="Projection scope controls">
        <span className="long-term-shortages__group-label" title="Compatible cached projections may be reused.">Projection scope · cached when compatible</span>
        <label>Horizon <select value={populationOptions.horizonWeeks} onChange={(event) => setPopulationOptions({ ...populationOptions, horizonWeeks: Number(event.target.value) as 13 | 26 | 52 | 72 })}>{[13, 26, 52, 72].map((weeks) => <option key={weeks} value={weeks}>{weeks} weeks</option>)}</select></label>
        <label><input type="checkbox" checked={populationOptions.includeManufacturedParts} onChange={(event) => setPopulationOptions({ ...populationOptions, includeManufacturedParts: event.target.checked })} /> Include Manufactured Parts</label>
      </div>
    </div>
    {alert && <div className={`long-term-shortages__banner ${error ? 'long-term-shortages__banner--error' : 'long-term-shortages__stale'}`} role="alert">{alert} {error && <button type="button" className="long-term-shortages__retry" onClick={retry}>Retry</button>}</div>}
     {rows.length === 0 ? <div className="long-term-shortages__state">{filters.showAll ? 'No components match the current filters.' : 'No components reach a shortage. Select Show All to view healthy components.'}</div> :
       <div className={`long-term-shortages__grid${replacing ? ' long-term-shortages__grid--refreshing' : ''}`} data-testid="workspace-shortages-grid" tabIndex={0} role="region" aria-label={`Workspace Shortages scrollable grid. Scroll horizontally to view ${data.weeks.length} Sunday through Saturday weeks.`}>
         <table className="long-term-shortages__matrix" aria-label="Workspace Shortages weekly balances" style={{ '--week-count': data.weeks.length } as CSSProperties}>
           <colgroup>
             {['comp', 'qad-status', 'description', 'kss', 'planner', 'on-hand', 'severity'].map((column) => <col key={column} className={`long-term-shortages__column--${column}`} />)}
             {data.weeks.map((week) => <col key={week.weekNumber} className="long-term-shortages__column--week" />)}
           </colgroup>
            <thead><tr><th scope="col">Comp</th><th scope="col" aria-label="QAD Status"><span aria-hidden="true">QAD<br />Status</span></th><th scope="col">Description</th><th scope="col">KSS</th><th scope="col">Buyer / Planner</th><th scope="col" className="long-term-shortages__num">On Hand</th><th scope="col">Severity</th>{data.weeks.map((week) => <th key={week.weekNumber} scope="col" title={week.weekStart}>Week {week.weekNumber}<br /><span className="long-term-shortages__week-date">{formatLongTermDate(week.labelDate)}</span></th>)}</tr></thead>
           <tbody>{rows.map((row) => <ShortageMatrixRow key={row.componentPart} row={row} onSelect={selectPart} />)}</tbody>
         </table>
       </div>}
    {selectedRow && <ComponentInfoModal key={selectedRow.componentPart} context="shortages" shortage={selectedRow} componentPart={selectedRow.componentPart}
      assignmentId={assignmentId} mpsSnapshotId={snapshotId} workspaceSite={site}
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
  const severityLabel = ({ CriticalShort: 'Critical Short', FutureShort: 'Future Short', SafetyStockShort: 'Safety Short', SafetyStockUnavailable: 'SS Unknown' } as Record<string, string>)[rowSeverity] ?? rowSeverity;
  const explanation = [row.safetyStock === null && rowSeverity === 'CriticalShort' ? 'Safety stock unknown.' : null,
    row.dataQualityWarning]
    .filter(Boolean).join(' ');
  return <tr aria-label={`${row.componentPart}: ${rowSeverity}`}><td className={`long-term-shortages__comp${severityClass}`}><button type="button" onClick={(event) => onSelect(row.componentPart, event.currentTarget)}>{row.componentPart}</button><span className="long-term-shortages__sr-only">, {rowSeverity}</span></td><td>{row.qadStatus ?? ''}</td><td title={row.description ?? undefined}>{row.description ?? ''}</td><td>{row.isKss ? 'KSS' : ''}</td><td title={row.planner ?? undefined}>{row.planner ?? ''}</td><td className="long-term-shortages__num">{row.openingDisplay}</td><td><SeverityLabel label={severityLabel} severity={rowSeverity} explanation={explanation} /></td>{row.ending.map((_, index) => <BucketCell key={index} row={row} index={index} />)}</tr>;
});

function SeverityLabel({ label, severity, explanation }: { label: string; severity: string; explanation: string }) {
  const tooltipId = useId();
  const [position, setPosition] = useState<{ top: number; left: number } | null>(null);
  const show = (element: HTMLElement) => {
    const bounds = element.getBoundingClientRect();
    setPosition({ top: Math.min(bounds.bottom + 4, window.innerHeight - 80), left: Math.max(8, Math.min(bounds.left, window.innerWidth - 328)) });
  };
  return <>
    <span className="long-term-shortages__severity-label shortage-severity" data-severity={severity} tabIndex={explanation ? 0 : undefined}
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

import { useRef, useState } from 'react';
import {
  DEFAULT_LONG_TERM_SHORTAGE_POPULATION_OPTIONS,
  exportLongTermShortages,
  type LongTermShortageRow,
} from '../api/longTermShortagesApi';
import { useLongTermShortages } from '../hooks/useLongTermShortages';
import {
  EMPTY_LONG_TERM_SHORTAGES_FILTERS,
  filterLongTermShortages,
  formatLongTermDate,
  formatLongTermQuantity,
} from '../longTermShortages/longTermShortagesPresentation';
import { saveLongTermShortagesWorkbook } from '../longTermShortages/saveLongTermShortagesWorkbook';
import './LongTermShortagesPanel.css';
import { LongTermShortageDetailModal } from './LongTermShortageDetailModal';

export function LongTermShortagesPanel({ assignmentId, snapshotId }: { assignmentId: string; snapshotId: string | null }) {
  const [populationOptions, setPopulationOptions] = useState(DEFAULT_LONG_TERM_SHORTAGE_POPULATION_OPTIONS);
  const { data, isLoading, error, retry } = useLongTermShortages(assignmentId, snapshotId, populationOptions);
  const [filters, setFilters] = useState(EMPTY_LONG_TERM_SHORTAGES_FILTERS);
  const [selectedRow, setSelectedRow] = useState<LongTermShortageRow | null>(null);
  const [isExporting, setIsExporting] = useState(false);
  const [exportError, setExportError] = useState<string | null>(null);
  const [exportSuccess, setExportSuccess] = useState<string | null>(null);
  const origin = useRef<HTMLButtonElement>(null);

  if (!snapshotId) return <div className="long-term-shortages__state">Load the MPS dashboard before viewing Workspace Shortages.</div>;
  if (isLoading) return <div className="long-term-shortages__state">Loading Workspace Shortages...</div>;
  if (error) return <div className="long-term-shortages__state long-term-shortages__state--error"><p>Workspace Shortages is unavailable: {error.detail}</p><button type="button" onClick={retry}>Retry Workspace Shortages</button></div>;
  if (!data) return null;
  const rows = filterLongTermShortages(data.rows, filters);

  return <section className="long-term-shortages" aria-labelledby="long-term-shortages-heading">
    <header className="long-term-shortages__toolbar">
      <h3 id="long-term-shortages-heading">Workspace Shortages</h3>
      <div className="long-term-shortages__filters">
        <label>Comp <input type="search" value={filters.component} onChange={(event) => setFilters({ ...filters, component: event.target.value })} /></label>
        <label>Planner <input type="search" value={filters.planner} onChange={(event) => setFilters({ ...filters, planner: event.target.value })} /></label>
        <label><input type="checkbox" checked={filters.kssOnly} onChange={(event) => setFilters({ ...filters, kssOnly: event.target.checked })} /> KSS</label>
        <label><input type="checkbox" checked={filters.statusOnly} onChange={(event) => setFilters({ ...filters, statusOnly: event.target.checked })} /> Status</label>
        <label><input type="checkbox" checked={filters.showAll} onChange={(event) => setFilters({ ...filters, showAll: event.target.checked })} /> Show All</label>
        <label><input type="checkbox" checked={populationOptions.includeManufacturedParts} onChange={(event) => setPopulationOptions({ ...populationOptions, includeManufacturedParts: event.target.checked })} /> Include Manufactured Parts</label>
        <label><input type="checkbox" checked={populationOptions.includePhantoms} onChange={(event) => setPopulationOptions({ ...populationOptions, includePhantoms: event.target.checked })} /> Include Phantoms</label>
      </div>
      <button type="button" disabled={rows.length === 0 || isExporting} onClick={async () => {
        setExportError(null);
        setExportSuccess(null);
        setIsExporting(true);
        try {
          const workbook = await exportLongTermShortages(assignmentId, data.snapshotId, rows.map((row) => row.componentPart), populationOptions);
          const result = await saveLongTermShortagesWorkbook(workbook.blob, workbook.fileName ?? `Workspace-Shortages-${data.refreshDate}.xlsx`);
          if (result.kind === 'saved') setExportSuccess(`Saved ${result.fileName}.`);
        } catch {
          setExportError('The workbook could not be prepared. Please retry.');
        } finally {
          setIsExporting(false);
        }
      }}>{isExporting ? 'Preparing workbook...' : 'Export displayed rows'}</button>
    </header>
    {data.isStale && <p className="long-term-shortages__stale" role="alert">Workspace Shortages is stale. {data.warning ?? 'Showing retained results.'} Refresh date: {data.refreshDate}.</p>}
    {exportError && <p className="long-term-shortages__state long-term-shortages__state--error" role="alert">{exportError}</p>}
    {exportSuccess && <p className="long-term-shortages__export-success" role="status">{exportSuccess}</p>}
    {rows.length === 0 ? <div className="long-term-shortages__state">{filters.showAll ? 'No components match the current filters.' : 'No components reach a shortage. Select Show All to view clear components.'}</div> :
      <div className="long-term-shortages__grid" data-testid="workspace-shortages-grid">
        <table className="long-term-shortages__metadata" aria-label="Workspace Shortages component metadata">
          <thead><tr><th scope="col">Comp</th><th scope="col">QAD Status</th><th scope="col">Description</th><th scope="col">KSS</th><th scope="col">Leadtime</th><th scope="col">Planner</th><th scope="col">On Hand</th></tr></thead>
          <tbody>{rows.map((row) => <ShortageMetadataRow key={row.componentPart} row={row} onSelect={(button) => { origin.current = button; setSelectedRow(row); }} />)}</tbody>
        </table>
        <div className="long-term-shortages__week-scroll" data-testid="workspace-shortages-week-scroll" tabIndex={0} aria-label="Workspace Shortages weekly balances. Scroll horizontally to view Weeks 1 through 24.">
          <table className="long-term-shortages__weeks" aria-label="Workspace Shortages weekly balances">
            <thead><tr>{rows[0].weeks.map((week) => <th key={week.weekNumber} scope="col" title={week.weekStart}>Week {week.weekNumber}<br />{formatLongTermDate(week.weekStart)}</th>)}</tr></thead>
            <tbody>{rows.map((row) => <ShortageWeekRow key={row.componentPart} row={row} />)}</tbody>
          </table>
        </div>
      </div>}
    {selectedRow && <LongTermShortageDetailModal row={selectedRow} onClose={() => { setSelectedRow(null); origin.current?.focus(); }} />}
  </section>;
}

function ShortageMetadataRow({ row, onSelect }: { row: LongTermShortageRow; onSelect: (button: HTMLButtonElement) => void }) {
  const currentSeverity = row.weeks[0]?.severity;
  const severityClass = currentSeverity === 'CriticalShort' ? ' long-term-shortages__comp--critical' : currentSeverity === 'SafetyStockShort' ? ' long-term-shortages__comp--safety' : '';
  const safetyStockUnresolved = row.safetyStockState === 'SelectedSiteValueMissing';
  return <tr aria-label={`${row.componentPart}: ${safetyStockUnresolved ? 'Safety stock unresolved' : row.severity}`}><td className={`long-term-shortages__comp${severityClass}`}><button type="button" onClick={(event) => onSelect(event.currentTarget)}>{row.componentPart}</button><span className="long-term-shortages__sr-only">, {safetyStockUnresolved ? 'Safety stock unresolved' : row.severity}</span></td><td>{row.qadStatus ?? ''}</td><td>{row.description ?? ''}</td><td>{row.isKss ? 'KSS' : ''}</td><td className="long-term-shortages__num">{formatLongTermQuantity(row.leadTimeWeeks)}</td><td>{row.planner ?? ''}</td><td className="long-term-shortages__num">{formatLongTermQuantity(row.displayOpeningQoh)}</td></tr>;
}

function ShortageWeekRow({ row }: { row: LongTermShortageRow }) {
  return <tr>{row.weeks.map((week) => <td key={week.weekNumber} className={`long-term-shortages__num${Number(week.balance) < 0 ? ' long-term-shortages__balance--negative' : ''}`} aria-label={`${row.componentPart}, Week ${week.weekNumber}: ${week.severity}, raw calculated balance ${week.balance}, displayed balance ${formatLongTermQuantity(week.displayBalance)}`}>{formatLongTermQuantity(week.displayBalance)}</td>)}</tr>;
}

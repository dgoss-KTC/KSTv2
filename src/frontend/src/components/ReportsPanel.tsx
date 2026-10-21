import { useEffect, useRef, useState } from 'react';
import { generateComponentMrp, type ComponentMrpRequest, type ComponentMrpPrepared } from '../api/componentMrpApi';
import { generateShipmentsToGo, type ShipmentsToGoRequest, type ShipmentsToGoPrepared } from '../api/shipmentsToGoApi';
import { generateSop, type SopRequest, type SopPrepared } from '../api/sopApi';
import { generateOpenOrderReport, type OpenOrderReportRequest, type OpenOrderReportPrepared } from '../api/openOrderReportApi';
import { fiscalChangedEvent, fiscalStorageKey, sopCalendarFingerprint } from '../reports/sopPresentation';
import { SopInputs } from '../reports/SopInputs';
import { OpenOrderReportInputs } from '../reports/OpenOrderReportInputs';
import { HistoricalShipmentsReport } from '../reports/HistoricalShipmentsReport';
import { PartInfoLookup } from '../reports/PartInfoLookup';
import type { MrpExportContext } from '../reports/partInfoMrpContext';
import { initialOor, oorOptional, oorPreferenceKey } from '../reports/openOrderReportPreferences';
import { WorkbookReportExport, type WorkbookKind } from '../reports/reportExportAdapters';
import { AnalyticalExcelExport, type ExcelExportAction } from './AnalyticalExcelExport';
import { useAnalyticalExportStatus } from '../shared/analyticalSaveRecovery';
import { ReportHead, ReportInfo } from '../reports/ReportChrome';
import { ReportColumnPicker } from '../reports/ReportColumnPicker';
import { openOrderColumnGroups } from '../reports/reportColumnGroups';
import './ReportsPanel.css';
import type { PartInfoShortcut } from '../reports/partInfoShortcut';

const sites = ['SW', 'NW', 'AR', 'MN', 'MS', 'KV'] as const;
const sopPreferenceKey = 'kst.sop.inputs.v1';
const stgPreferenceKey = 'kst.shipmentsToGo.inputs.v1';
type ReportKind = Exclude<WorkbookKind, 'ComponentMrp'> | 'HistoricalShipments' | 'PartInfo';
const titles: Record<ReportKind, string> = { ShipmentsToGo: 'Shipments-To-Go', Sop: 'S&OP', OpenOrderReport: 'Open Order Report', PartInfo: 'Part Info & MRP', HistoricalShipments: 'Historical Shipments' };
function initialSop(): SopRequest {
  try {
    const v = JSON.parse(localStorage.getItem(sopPreferenceKey) ?? 'null') as SopRequest | null;
    if (v && sites.includes(v.site as typeof sites[number]) && ['planr', 'customer'].includes(v.groupBy) && Array.isArray(v.ranges) && v.ranges.length >= 1 && v.ranges.length <= 5 && v.ranges.every(r => r && /^[0-9]{0,4}$/.test(r.from) && /^[0-9]{0,4}$/.test(r.to))) return v;
  } catch { /* First-use defaults. */ }
  return { site: 'SW', groupBy: 'planr', ranges: [{ from: '', to: '' }] };
}
function initialStg() {
  try {
    const v = JSON.parse(localStorage.getItem(stgPreferenceKey) ?? 'null') as { sites?: unknown; includeNextWeek?: unknown } | null;
    if (v && Array.isArray(v.sites) && v.sites.every(s => sites.includes(s)) && new Set(v.sites).size === v.sites.length) return { sites: v.sites as string[], includeNextWeek: v.includeNextWeek === true };
  } catch { /* First-use defaults. */ }
  return { sites: ['SW', 'NW', 'KV'], includeNextWeek: false };
}

export function ReportsPanel({ generate = generateComponentMrp, generateStg = generateShipmentsToGo, generateSopReport = generateSop, generateOor = generateOpenOrderReport, onBusyChange, active = true, shortcut, onBackToMps }: {
  shortcut?: PartInfoShortcut | null;
  onBackToMps?: () => void;
  generate?: (request: ComponentMrpRequest, signal?: AbortSignal, epoch?: number) => Promise<ComponentMrpPrepared>;
  generateStg?: (request: ShipmentsToGoRequest, signal?: AbortSignal, epoch?: number) => Promise<ShipmentsToGoPrepared>;
  generateSopReport?: (request: SopRequest, signal?: AbortSignal, epoch?: number) => Promise<SopPrepared>;
  generateOor?: (request: OpenOrderReportRequest, signal?: AbortSignal, epoch?: number) => Promise<OpenOrderReportPrepared>;
  onBusyChange?: (busy: boolean) => void; active?: boolean;
}) {
  const [kind, setKind] = useState<ReportKind>('PartInfo');
  const [mrp, setMrp] = useState<MrpExportContext | null>(null);
  const [shortcutToken, setShortcutToken] = useState<number | undefined>(undefined);
  if (shortcut && shortcut.token !== shortcutToken) {
    setShortcutToken(shortcut.token); setKind('PartInfo'); setMrp(null);
  }
  const [stg, setStg] = useState(initialStg); const [sop, setSop] = useState(initialSop); const [oor, setOor] = useState(initialOor);
  const [calendarVersion, setCalendarVersion] = useState(0); const [message, setMessage] = useState(''); const [warning, setWarning] = useState('');
  const action = useRef<ExcelExportAction>(null);
  const status = useAnalyticalExportStatus();
  const busy = status.operations.some(o => o.pending || o.opening);
  useEffect(() => { onBusyChange?.(busy); }, [busy, onBusyChange]);
  useEffect(() => {
    const changed = () => setCalendarVersion(n => n + 1);
    const storage = (e: StorageEvent) => { if (e.key === fiscalStorageKey) changed(); };
    window.addEventListener(fiscalChangedEvent, changed); window.addEventListener('storage', storage);
    return () => { window.removeEventListener(fiscalChangedEvent, changed); window.removeEventListener('storage', storage); };
  }, []);
  function remember(key: string, value: unknown) {
    setMessage('');
    try { localStorage.setItem(key, typeof value === 'string' ? value : JSON.stringify(value)); }
    catch { setWarning('Report inputs could not be remembered; the session choice is retained.'); }
  }
  const changeStg = (v: typeof stg) => { setStg(v); remember(stgPreferenceKey, v); };
  const changeSop = (v: SopRequest) => { setSop(v); remember(sopPreferenceKey, v); };
  const changeOor = (v: OpenOrderReportRequest) => { setOor(v); remember(oorPreferenceKey, v.optionalFields); };
  let fiscal = ''; let fiscalError = '';
  try { fiscal = sopCalendarFingerprint(); } catch { fiscalError = 'Fiscal calendar unavailable. Check Settings.'; }
  const sopRequest = () => ({ ...sop, ranges: sop.ranges.filter(r => r.from || r.to) });
  const common = { actionRef: action, preparingMessage: 'Acquiring fresh report facts…', onMessage: setMessage };
  const invalidSop = !sop.ranges.some(r => r.from || r.to) || sop.ranges.some(r => !!(r.from || r.to) && (!/^[0-9]{4}$/.test(r.from) || !/^[0-9]{4}$/.test(r.to)));
  const mrpReason = !mrp ? 'Look up a part first.' : mrp.state === 'loading' ? 'MRP is loading.' : mrp.state === 'error' ? 'MRP unavailable. Retry MRP.' : mrp.state === 'empty' ? 'No MRP data to export.' : undefined;
  const exportControl = kind === 'PartInfo' ? <AnalyticalExcelExport retainPreparedOnDeparture label="Component MRP" contextKey={JSON.stringify(mrp?.request ?? null)} request={() => { if (!mrp || mrp.state !== 'ready') throw new Error('Refresh MRP before exporting.'); return mrp.request; }} scopeLabel={mrp ? `Exports the MRP shown · ${mrp.request.site} · ${mrp.request.part} · ${mrp.request.horizonWeeks} weeks by ${mrp.request.period} · unconfirmed POs ${mrp.request.includeUnconfirmed ? 'included' : 'excluded'}` : ''} disabledReason={mrpReason} prepareDisabledReason={mrp?.expired ? 'MRP acquisition expired. Refresh MRP before exporting.' : undefined} clientFactory={() => new WorkbookReportExport('ComponentMrp', generate)} />
    : kind === 'ShipmentsToGo' ? <AnalyticalExcelExport {...common} label={titles[kind]} contextKey={JSON.stringify(stg)} request={() => stg} scopeLabel={`${stg.sites.join(', ')} · this week${stg.includeNextWeek ? ' and next week' : ''}`} disabledReason={!stg.sites.length ? 'Select at least one site.' : undefined} clientFactory={() => new WorkbookReportExport('ShipmentsToGo', generateStg)} />
    : kind === 'Sop' ? <AnalyticalExcelExport {...common} label={titles[kind]} contextKey={JSON.stringify([sop, fiscal, calendarVersion])} request={() => ({ ...sopRequest(), fiscalFingerprint: sopCalendarFingerprint() })} scopeLabel={`${sop.site} · by ${sop.groupBy === 'planr' ? 'Planner ID' : 'Customer Name'} · ${sopRequest().ranges.length} product-line ranges`} disabledReason={fiscalError || (invalidSop ? 'Enter at least one product-line range, four digits in each box.' : undefined)} clientFactory={() => new WorkbookReportExport('Sop', generateSopReport)} />
    : kind === 'OpenOrderReport' ? <AnalyticalExcelExport {...common} label={titles[kind]} contextKey={JSON.stringify(oor)} request={() => oor} scopeLabel={`${[oor.customerName && `Customer “${oor.customerName}”`, oor.customerNumber && `Customer ${oor.customerNumber}`, oor.site, oor.salespersonId && `Salesperson ${oor.salespersonId}`, oor.ios && `IOS ${oor.ios}`, (oor.productLineFrom || oor.productLineTo) && `${oor.productLineFrom} to ${oor.productLineTo}`, (oor.dueDateFrom || oor.dueDateTo) && `${oor.dueDateFrom} to ${oor.dueDateTo}`].filter(Boolean).join(' · ')} · ${oor.optionalFields.length} optional columns`} disabledReason={![oor.customerName, oor.customerNumber, oor.salespersonId, oor.productLineFrom, oor.productLineTo, oor.ios, oor.site].some(v => v?.trim()) ? 'Enter a customer, salesperson, IOS, product line or site.' : undefined} clientFactory={() => new WorkbookReportExport('OpenOrderReport', generateOor)} /> : null;

  return <section className={`reports-panel${kind === 'PartInfo' ? ' reports-panel--part-info' : ''}`} aria-label="Reports">
    {onBackToMps && <div className="reports-panel__origin"><button type="button" className="report-button" onClick={onBackToMps}>← Back to MPS</button>
      <span>Independent current lookup; not part of the originating MPS snapshot.</span></div>}
    <nav className="reports-panel__selector" aria-label="Report selector"><span>Workbooks</span>{(Object.keys(titles) as ReportKind[]).map(k => <span className="reports-panel__selector-item" key={k}>{k === 'PartInfo' && <span className="reports-panel__group">On screen</span>}<button aria-current={kind === k ? 'page' : undefined} onClick={() => { if (k !== kind) { setKind(k); setMrp(null); setMessage(''); } }}>{titles[k]}</button></span>)}</nav>
    <div className="reports-panel__frame">
      {kind === 'HistoricalShipments' ? <HistoricalShipmentsReport active={active} onBusyChange={onBusyChange} /> : <>
        <ReportHead title={titles[kind]} description={kind === 'PartInfo' ? 'Look up one exact part number at a supported site.' : 'Independent report generation outside workspace populations.'}>
          {kind === 'OpenOrderReport' && <><ReportInfo title="About these filters"><p>Customer Name matches part of a name; use % for any characters and _ for one character. Other text filters match exactly.</p><p>Both dates or neither. MM/DD/YYYY, MM/DD/YY or MM/DD (current year); . and - separators are supported. End must be on or after start.</p><p>Enter at least one non-date filter. An explicit site qualifies; All supported sites or dates alone do not.</p></ReportInfo><ReportColumnPicker groups={openOrderColumnGroups()} selected={oor.optionalFields} disabled={busy} onReset={() => changeOor({ ...oor, optionalFields: [] })} onChange={ids => changeOor({ ...oor, optionalFields: oorOptional.filter(id => ids.includes(id)) })} /></>}
          {active && exportControl}
        </ReportHead>
        <div className="report-body">{kind === 'PartInfo' ? <PartInfoLookup key={String(active)} active={active} onExportContext={setMrp} shortcut={shortcut} /> : <form className="report-workbook" onSubmit={e => { e.preventDefault(); action.current?.run(); }}>
          <fieldset disabled={busy}>
            {kind === 'Sop' ? <SopInputs value={sop} onChange={changeSop} /> : kind === 'OpenOrderReport' ? <OpenOrderReportInputs value={oor} onChange={changeOor} /> : <><section><h2>Sites <span><button type="button" onClick={() => changeStg({ ...stg, sites: [...sites] })}>All</button><button type="button" onClick={() => changeStg({ ...stg, sites: [] })}>None</button></span></h2><div className="report-sites" role="group" aria-label="Select sites">{sites.map(s => <label key={s}><input type="checkbox" checked={stg.sites.includes(s)} onChange={e => changeStg({ ...stg, sites: e.target.checked ? [...stg.sites, s] : stg.sites.filter(v => v !== s) })} />{s}</label>)}</div></section><section><h2>Options</h2><label className="report-check"><input type="checkbox" checked={stg.includeNextWeek} onChange={e => changeStg({ ...stg, includeNextWeek: e.target.checked })} />Include Next Week?</label></section></>}
          </fieldset><button type="submit" className="report-enter-submit" tabIndex={-1} aria-hidden="true">Export workbook</button>
        </form>}</div>
        {(message || warning) && <footer className="report-foot"><span role="status">{message}</span>{warning && <span role="alert">{warning}</span>}</footer>}
      </>}
    </div>
  </section>;
}

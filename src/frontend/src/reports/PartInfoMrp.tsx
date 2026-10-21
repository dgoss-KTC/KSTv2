import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { readComponentMrp, type ComponentMrpRequest, type ComponentMrpScreen } from '../api/componentMrpApi';
import { formatLongTermQuantity } from '../longTermShortages/longTermShortagesPresentation';
import { isNegativeDecimal } from '../shared/exactDecimal';
import { Segmented } from './ReportChrome';
import './PartInfoMrp.css';
import type { MrpSettings, MrpExportContext } from './partInfoMrpContext';

const metrics = [['Gross Requirements', 'demand'], ['Scheduled Receipts', 'receipts'], ['Projected QOH', 'projectedQoh'],
  ['Planned Orders Due', 'plannedDue'], ['Planned Orders Release', 'plannedRelease']] as const;

/** Independent acquisition. Server periods/balances are rendered without client netting. */
export function PartInfoMrp({ site, part, settings, onSettings, onExportContext, loadMrp = readComponentMrp }: {
  site: string; part: string; settings: MrpSettings; onSettings: (value: MrpSettings) => void;
  onExportContext?: (context: MrpExportContext | null) => void; loadMrp?: typeof readComponentMrp;
}) {
  const [data, setData] = useState<ComponentMrpScreen | null>(null);
  const retained = useRef<ComponentMrpScreen | null>(null);
  const [outcome, setOutcome] = useState({ key: '', error: '' });
  const [refresh, setRefresh] = useState(0);
  const fresh = useRef(true);
  const [expiredClock, setExpiredClock] = useState('');
  const { horizonWeeks, period, includeUnconfirmed } = settings;
  const requestKey = JSON.stringify([site, part, horizonWeeks, period, includeUnconfirmed, refresh]);
  useEffect(() => {
    const controller = new AbortController(); let current = true;
    const timer = window.setTimeout(() => {
      const held = retained.current;
      const reuse = !fresh.current && held?.site === site && held.part === part && held.horizonWeeks === horizonWeeks;
      fresh.current = false;
      const request: ComponentMrpRequest = { site, part, horizonWeeks, period, includeUnconfirmed,
        ...(reuse ? { acquisitionId: held.acquisitionId, asOfDate: held.asOfDate } : {}) };
      // A superseded fresh acquisition may replace server retention. Never reuse the old token then.
      if (!reuse) retained.current = null;
      void loadMrp(request, controller.signal).then(result => {
        if (!current || controller.signal.aborted) return;
        if (result.site !== site || result.part !== part || result.horizonWeeks !== horizonWeeks || result.period !== period
          || result.includeUnconfirmed !== includeUnconfirmed || !result.acquisitionId || !result.expiresAtUtc
          || !Array.isArray(result.periods)) throw new Error('MRP identity/settings mismatch. Retry MRP.');
        retained.current = result; setData(result); setOutcome({ key: requestKey, error: '' });
        if (Date.parse(result.expiresAtUtc) <= Date.now()) setExpiredClock(JSON.stringify([result.acquisitionId, result.expiresAtUtc]));
      }).catch(failure => { if (current && !controller.signal.aborted) setOutcome({ key: requestKey, error: failure instanceof Error ? failure.message : 'MRP could not be acquired. Retry MRP.' }); });
    }, 150);
    return () => { current = false; controller.abort(); window.clearTimeout(timer); };
  }, [site, part, horizonWeeks, period, includeUnconfirmed, refresh, requestKey, loadMrp]);
  const clockKey = JSON.stringify([data?.acquisitionId, data?.expiresAtUtc]);
  const expired = !!data && expiredClock === clockKey;
  useEffect(() => {
    if (!data) return;
    const today = new Date(); const midnight = new Date(today.getFullYear(), today.getMonth(), today.getDate() + 1).getTime();
    const remaining = Math.min(Date.parse(data.expiresAtUtc), midnight) - Date.now();
    const timer = window.setTimeout(() => setExpiredClock(clockKey), Math.max(0, remaining));
    return () => window.clearTimeout(timer);
  }, [data, clockKey]);
  const error = outcome.key === requestKey ? outcome.error : '';
  const state = outcome.key !== requestKey ? 'loading' : error ? 'error' : !data?.periods.length ? 'empty' : 'ready';
  useLayoutEffect(() => {
    onExportContext?.({ state, expired, request: { site, part, horizonWeeks, period, includeUnconfirmed,
      acquisitionId: data?.acquisitionId ?? null, asOfDate: data?.asOfDate ?? null } });
  }, [onExportContext, state, expired, site, part, horizonWeeks, period, includeUnconfirmed, data?.acquisitionId, data?.asOfDate]);
  useLayoutEffect(() => () => onExportContext?.(null), [onExportContext]);
  function refreshMrp() { fresh.current = true; setRefresh(n => n + 1); }
  const qty = (value: string | null) => formatLongTermQuantity(value, data?.unitOfMeasure);
  return <section className="part-info-mrp" aria-label="Component MRP">
    <header className="part-info-mrp__caption"><h3>Component MRP</h3>{data && <>
      <span>As of {data.asOfDate}</span><span>{data.horizonWeeks} weeks by {data.period}</span>
      <span>Adjusted opening {qty(data.adjustedOpeningQoh)} {data.unitOfMeasure}</span>
      {data.safetyStock !== null && <span>Safety stock {qty(data.safetyStock)}</span>}
      <span>Acquired <time dateTime={data.acquiredAtUtc}>{new Date(data.acquiredAtUtc).toLocaleString()}</time></span>
      <span>{data.period === 'Day' ? 'Calendar days' : data.period === 'Week' ? 'First week partial; Monday labels' : '* Partial calendar month'}</span>
      {data.includeUnconfirmed && <span>† includes unconfirmed PO receipts</span>}
    </>}<button className="report-button" onClick={refreshMrp}>Refresh MRP</button></header>
    <div className="part-info-mrp__controls">
      <Segmented label="Horizon" value={String(horizonWeeks)} choices={[13, 26, 52, 72].map(n => ({ value: String(n), label: String(n) }))} onChange={v => onSettings({ ...settings, horizonWeeks: Number(v) })} />
      <Segmented label="Period" value={period} choices={['Day', 'Week', 'Month'].map(v => ({ value: v, label: v }))} onChange={v => onSettings({ ...settings, period: v })} />
      <label className={`part-info-mrp__check${includeUnconfirmed ? ' part-info-mrp__check--checked' : ''}`}><input type="checkbox" checked={includeUnconfirmed} onChange={e => onSettings({ ...settings, includeUnconfirmed: e.target.checked })} />Include Unconfirmed POs</label>
    </div>
    {state === 'loading' && <p role="status" className={!data ? 'part-info-mrp__initial' : undefined}>Acquiring MRP…</p>}
    {error && <p role="alert">{error} <button className="report-button" onClick={refreshMrp}>Retry MRP</button></p>}
    {expired && <p className="part-info-mrp__warning">MRP acquisition expired. Refresh MRP before exporting.</p>}
    {data?.dataQualityWarning && <p className="part-info-mrp__warning" role="status">{data.dataQualityWarning}</p>}
    {state === 'empty' && <p>No Component MRP data found.</p>}
    {data && data.periods.length > 0 && <div className={`part-info-mrp__scroll${state === 'loading' ? ' part-info-mrp__scroll--loading' : ''}`} role="region" tabIndex={0} aria-label="Scrollable Component MRP periods" aria-busy={state === 'loading'}>
      <table aria-label="Component MRP periods"><thead><tr><th scope="col">Metric</th>{data.periods.map((p, i) => <th scope="col" key={i} title={`${p.start ?? 'Past Due'} to ${p.endExclusive ?? ''} (exclusive)`}>{p.label}</th>)}</tr></thead>
        <tbody>{metrics.map(([label, key]) => <tr key={key}><th scope="row">{label}</th>{data.periods.map((p, i) => <td key={i} className={key === 'projectedQoh' && isNegativeDecimal(p[key]) ? 'part-info-mrp__negative' : undefined}>
          {qty(p[key])}{key === 'receipts' && p.hasUnconfirmed && data.includeUnconfirmed && <>†<span className="part-info-mrp__sr-only"> includes unconfirmed receipts</span></>}
        </td>)}</tr>)}</tbody>
      </table>
    </div>}
  </section>;
}

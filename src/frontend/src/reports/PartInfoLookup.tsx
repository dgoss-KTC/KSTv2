import { useEffect, useEffectEvent, useRef, useState } from 'react';
import type { PartInfoShortcut } from './partInfoShortcut';
import { getPartInfoSpec, lookupPartInfo, refreshPartInfoLocations, PartInfoError, type PartInfo, type PartInfoSpec } from '../api/partInfoApi';
import { formatBuyerCommentMetadata } from '../shared/buyerCommentPresentation';
import { decimal, exactText, formatDecimal } from '../shared/exactDecimal';
import { PartIdentity, FactsStrip, Fields, InfoCard as Card, ScrollTable, InfoTabs } from '../shared/partInfoParts';
import { SupplierStrip } from '../shared/SupplierStrip';
import { PartInfoMrp } from './PartInfoMrp';
import { defaultMrpSettings, type MrpExportContext } from './partInfoMrpContext';
import { readComponentMrp } from '../api/componentMrpApi';
import './PartInfoLookup.css';

const absent = '—';
const flag = (value: boolean | null) => value === true ? 'Yes' : value === false ? 'No' : 'Unknown';
const quantity = (value: string | number | null | undefined) => value == null ? absent : String(value);
const days = (value: string | number | null | undefined) => value == null ? absent : `${value} days`;
const price = (value: string | null) => value === null ? absent : `$${formatDecimal(value, 4)}`;
function inventoryQuantity(value: string | null): string {
  if (value === null) return absent;
  const parsed = decimal(value);
  return parsed ? exactText(parsed) : value;
}

function BuyerCommentDialog({ part, text, recordedAt, author, onClose }: { part: string; text: string; recordedAt?: string | null; author?: string | null; onClose: () => void }) {
  const close = useRef<HTMLButtonElement>(null);
  useEffect(() => {
    const opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    close.current?.focus();
    return () => { if (opener?.isConnected) opener.focus(); };
  }, []);
  return <div className="part-info__backdrop" onClick={event => { if (event.target === event.currentTarget) onClose(); }}>
    <section className="part-info__dialog" role="dialog" aria-modal="true" aria-labelledby="part-info-comment-title"
      onKeyDown={event => {
        if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); onClose(); }
        if (event.key === 'Tab') {
          const controls = [...event.currentTarget.querySelectorAll<HTMLElement>('button, [tabindex="0"]')];
          const first = controls[0]; const last = controls[controls.length - 1];
          if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
          else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        }
      }}>
      <header><h2 id="part-info-comment-title">Buyer comment · {part}</h2><button ref={close} onClick={onClose} aria-label="Close buyer comment">Close</button></header>
      <p className="buyer-comment-metadata">{formatBuyerCommentMetadata(recordedAt, author)}</p>
      <div className="part-info__comment" tabIndex={0} role="region" aria-label="Full buyer comment">{text || 'No comment text in latest entry.'}</div>
    </section>
  </div>;
}

const tabs = [{ id: 'orders', label: 'Open POs' }, { id: 'locations', label: 'Inventory lots' }, { id: 'alternates', label: 'Approved alternates' }] as const;
type Tab = typeof tabs[number]['id'];

export function PartInfoLookup({ active = true, lookup = lookupPartInfo, loadSpec = getPartInfoSpec, refreshLocations = refreshPartInfoLocations, loadMrp = readComponentMrp, onExportContext, shortcut }: {
  active?: boolean;
  shortcut?: PartInfoShortcut | null;
  lookup?: typeof lookupPartInfo;
  loadSpec?: typeof getPartInfoSpec;
  refreshLocations?: typeof refreshPartInfoLocations;
  loadMrp?: typeof readComponentMrp;
  onExportContext?: (context: MrpExportContext | null) => void;
}) {
  const [spec, setSpec] = useState<PartInfoSpec | null>(null);
  const [specError, setSpecError] = useState(false);
  const [specAttempt, setSpecAttempt] = useState(0);
  const [site, setSite] = useState('SW');
  const [part, setPart] = useState('');
  const [report, setReport] = useState<PartInfo | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [missing, setMissing] = useState(false);
  const [inventoryLoading, setInventoryLoading] = useState(false);
  const [tab, setTab] = useState<Tab>('orders');
  const [commentOpen, setCommentOpen] = useState(false);
  const [mrpSettings, setMrpSettings] = useState(defaultMrpSettings);
  const inventoryEpoch = useRef(0);
  const inventoryAbort = useRef<AbortController | null>(null);
  const epoch = useRef(0);
  const abort = useRef<AbortController | null>(null);
  useEffect(() => {
    const controller = new AbortController(); let current = true;
    void loadSpec(controller.signal).then(value => { if (current) {
      setSpec(value); setSpecError(false);
      try { const saved = localStorage.getItem('kst.componentMrp.site.v1'); setSite(saved && value.sites.includes(saved) ? saved : value.sites.includes('SW') ? 'SW' : value.sites[0]); }
      catch { setSite(value.sites.includes('SW') ? 'SW' : value.sites[0]); }
    } })
      .catch(() => { if (current) setSpecError(true); });
    return () => { current = false; controller.abort(); };
  }, [loadSpec, specAttempt]);
  useEffect(() => {
    const generation = epoch;
    const request = abort;
    const inventoryGeneration = inventoryEpoch;
    const inventoryRequest = inventoryAbort;
    if (!active) { generation.current++; request.current?.abort(); inventoryGeneration.current++; inventoryRequest.current?.abort(); }
    return () => { generation.current++; request.current?.abort(); inventoryGeneration.current++; inventoryRequest.current?.abort(); };
  }, [active]);
  function invalidate() {
    setCommentOpen(false); setTab('orders');
    inventoryEpoch.current++; inventoryAbort.current?.abort(); setInventoryLoading(false);
    epoch.current++; abort.current?.abort(); setReport(null); setLoading(false); setError(null); setMissing(false);
  }
  async function submit(preserveMrp = false, incoming?: { part: string; site: string }) {
    const requestedPart = (incoming?.part ?? part).trim(); const requestedSite = incoming?.site ?? site;
    if (!active || !spec || !requestedPart) return;
    if (!spec.sites.includes(requestedSite) || requestedPart.length > Number(spec.maximumPartLength)) {
      invalidate(); setError('Select a supported site and a valid part number.'); return;
    }
    if (!preserveMrp) setMrpSettings(defaultMrpSettings);
    setCommentOpen(false); setTab('orders');
    inventoryEpoch.current++; inventoryAbort.current?.abort(); setInventoryLoading(false);
    const token = ++epoch.current; abort.current?.abort();
    const controller = new AbortController(); abort.current = controller;
    setLoading(true); setReport(null); setError(null); setMissing(false);
    try {
      const result = await lookup(requestedSite, requestedPart, controller.signal);
      if (epoch.current !== token || controller.signal.aborted) return;
      if (result.site !== requestedSite || result.part !== requestedPart) throw new Error('Lookup identity mismatch. Please retry.');
      setReport(result);
    } catch (failure) {
      if (epoch.current === token && !controller.signal.aborted) {
        setMissing(failure instanceof PartInfoError && failure.status === 404);
        setError(failure instanceof Error ? failure.message : 'Lookup unavailable. Please retry.');
      }
    } finally { if (epoch.current === token) setLoading(false); }
  }
  const applyShortcut = useEffectEvent(() => {
    if (!active || !spec || !shortcut?.claim()) return;
    setPart(shortcut.part); setSite(shortcut.site);
    void submit(false, shortcut);
    document.getElementById('part-info-part')?.focus();
  });
  useEffect(() => {
    let current = true;
    queueMicrotask(() => { if (current) applyShortcut(); });
    return () => { current = false; };
  }, [active, spec, shortcut]);
  async function refreshInventory() {
    if (!report || !active) return;
    const identity = report;
    const lookupToken = epoch.current;
    const token = ++inventoryEpoch.current; inventoryAbort.current?.abort();
    const controller = new AbortController(); inventoryAbort.current = controller; setInventoryLoading(true);
    const current = () => epoch.current === lookupToken && inventoryEpoch.current === token && !controller.signal.aborted;
    try {
      const result = await refreshLocations(identity.site, identity.part, controller.signal);
      if (!current()) return;
      if (result.site !== identity.site || result.part !== identity.part) throw new Error('Inventory lookup identity mismatch.');
      setReport(previous => previous?.site === identity.site && previous.part === identity.part ? { ...previous, locations: result.locations } : previous);
    } catch {
      if (current()) setReport(previous => previous?.site === identity.site && previous.part === identity.part
        ? { ...previous, locations: { value: null, acquiredAtUtc: null, error: 'Inventory locations could not be acquired. Retry inventory.' } } : previous);
    } finally { if (current()) setInventoryLoading(false); }
  }
  const m = report?.master;
  return <div className="part-info">
    {specError && <p role="alert">Lookup settings unavailable. <button onClick={() => setSpecAttempt(v => v + 1)}>Retry settings</button></p>}
    <form className="part-info__inputs" onSubmit={event => { event.preventDefault(); void submit(); }}>
      <label htmlFor="part-info-part">Part Number<input id="part-info-part" autoComplete="off" value={part} maxLength={Number(spec?.maximumPartLength ?? 256)}
        onChange={event => { invalidate(); setPart(event.target.value); }} /></label>
      <label htmlFor="part-info-site">Site<select id="part-info-site" value={site} disabled={!spec}
        onChange={event => { invalidate(); setSite(event.target.value); try { localStorage.setItem('kst.componentMrp.site.v1', event.target.value); } catch { /* Session selection remains usable. */ } }}>{(spec?.sites ?? ['SW']).map(s => <option key={s}>{s}</option>)}</select></label>
      <button type="submit" className="report-button report-button--primary" disabled={!spec || !part.trim() || !active}>{loading ? 'Look up again' : 'Lookup'}</button>
      {loading && <button type="button" className="report-button" onClick={invalidate}>Cancel lookup</button>}
    </form>
    {loading && <p role="status">Acquiring part information…</p>}
    {error && <div role={missing ? 'status' : 'alert'} className="part-info__state"><h2>{missing ? 'Part not found' : 'Lookup unavailable'}</h2><p>{error}</p><button onClick={() => void submit(true)}>Retry lookup</button></div>}
    {!loading && !error && !report && <p className="part-info__state">Enter a part number and select Lookup, or press Enter.</p>}
    {report && m && <>
      <PartIdentity part={report.part} description={m.description ?? 'Description not available'} chips={[
        <strong key="site" className="report-chip">Site {report.site}</strong>,
        <span key="status" className="report-chip">{m.partStatusCode ? `${m.partStatusCode}${m.partStatusDescription ? ` — ${m.partStatusDescription}` : ''}` : 'Part status unavailable'}</span>,
        ]} actions={<button className="report-button" aria-label="Refresh / Retry sections" title="Refresh all sections; retries any that failed" onClick={() => void submit(true)}>Refresh</button>} />
      <FactsStrip items={[
          ['Net QOH', report.inventory.value ? inventoryQuantity(report.inventory.value.netQuantityOnHand) : 'Unavailable'],
          ['Std / QCTC', <><span>{price(m.standardCost)}</span> / <span>{price(m.qctc)}</span></>],
          ['Purchase LT', days(m.purchaseLeadTimeDays)],
          ['Min / multiple', `${quantity(m.minimumOrderQuantity)} / ${quantity(m.orderMultiple)}`],
          ['Safety time', days(m.safetyTime)],
          ['Buyer / IOS', `${m.buyerPlanner ?? absent} / ${m.iosCode ?? absent}`],
        ]} />
      {report.inventory.error && <p role="alert">{report.inventory.error}</p>}
      <details className="part-info__more"><summary>More part details</summary><div className="part-info__details-grid">
          <Card title="Inventory / Planning" acquired={report.inventory.acquiredAtUtc}>
            <Fields values={[["Non-Net QOH", report.inventory.value ? inventoryQuantity(report.inventory.value.nonNetQuantityOnHand) : 'Unavailable'], ["Time Fence", quantity(m.timeFence)], ["Safety Stock", quantity(m.safetyStock)]]} /></Card>
          <Card title="Additional lead times"><Fields values={[["Inspection LT", days(m.inspectionLeadTimeDays)], ["Cumulative LT", days(m.cumulativeLeadTimeDays)]]} /></Card>
          <Card title="Manufacturer item" error={report.context.error} acquired={report.context.acquiredAtUtc}>
            {!report.context.error && <p>{report.context.value?.manufacturerItem ?? 'No manufacturer item available from qualifying confirmed PO lines.'}</p>}
            {report.context.value?.effectivePmCode === 'M' && <Fields values={[["Manufacturing Lead (site working days)", quantity(report.context.value.manufacturingLeadWorkingDays)]]} />}</Card>
      </div></details>
      <SupplierStrip section={report.suppliers} />
      <section className="part-info__comment-strip" aria-label="Current buyer comment">
        {report.comment.error ? <p role="alert">{report.comment.error}</p> : report.comment.value || report.commentRecordedAt ?
          <button className="part-info__comment-preview" onClick={() => setCommentOpen(true)} aria-haspopup="dialog">
            <strong>Buyer comment</strong><span className="buyer-comment-metadata">{formatBuyerCommentMetadata(report.commentRecordedAt, report.commentAuthor)}</span><span className="part-info__comment-preview-text">{report.comment.value || 'No comment text in latest entry.'}</span><span>Read full comment</span>
          </button> : <p>No current buyer comment.</p>}
        {report.comment.acquiredAtUtc && <p className="part-info__time">Acquired <time dateTime={report.comment.acquiredAtUtc}>{new Date(report.comment.acquiredAtUtc).toLocaleString()}</time></p>}
      </section>
      {active && <PartInfoMrp site={report.site} part={report.part} settings={mrpSettings} onSettings={setMrpSettings} loadMrp={loadMrp} onExportContext={onExportContext} />}
      <InfoTabs idPrefix="part-info" tabs={tabs.map(item => ({ ...item, count: report[item.id].value?.length ?? 'Unavailable', failed: !!report[item.id].error }))} active={tab} onChange={setTab} />
      <div id="part-info-panel-alternates" role="tabpanel" aria-labelledby="part-info-tab-alternates" hidden={tab !== 'alternates'}>
          <Card title="Approved Alternates" error={report.alternates.error} acquired={report.alternates.acquiredAtUtc}>
              {report.alternates.value && (report.alternates.value.length === 0 ? <p>No approved alternates found.</p> : <ScrollTable label="Approved Alternates" headings={['Supplier', 'Vendor Name', 'Supplier Item', 'MFG Part']}
                 rows={report.alternates.value.map(v => [v.supplier, v.vendorName, v.supplierItem, v.manufacturerPart])} />)}</Card>
      </div>
      <div id="part-info-panel-locations" role="tabpanel" aria-labelledby="part-info-tab-locations" hidden={tab !== 'locations'}>
        <Card title="Inventory / Lot Locations" error={report.locations.error} acquired={report.locations.acquiredAtUtc}>
          <button disabled={inventoryLoading} onClick={() => void refreshInventory()}>{report.locations.error ? 'Retry inventory' : 'Refresh inventory'}</button>
          {inventoryLoading && <p role="status">Refreshing inventory locations…</p>}
          {report.locations.value && (report.locations.value.length === 0 ? <p>No inventory locations found.</p> : <ScrollTable label="Inventory / Lot Locations" headings={['Location', 'Lot / Serial', 'Status', 'Qty on Hand']}
             numeric={[3]} rows={report.locations.value.map(r => [r.location, r.lotSerial, r.status, inventoryQuantity(r.quantityOnHand)])} />)}</Card>
      </div>
      <div id="part-info-panel-orders" role="tabpanel" aria-labelledby="part-info-tab-orders" hidden={tab !== 'orders'}>
          <Card title="Open purchase orders" error={report.orders.error} acquired={report.orders.acquiredAtUtc}>
            {report.orders.value && (report.orders.value.length === 0 ? <p>No qualifying open purchase orders found.</p> : <ScrollTable label="Open purchase orders" headings={['PO', 'Line', 'Due Date', 'Open Qty', 'Confirmed', 'Supplier', 'Buyer', 'Manufacturer Item', 'Tracking']}
               numeric={[1, 3]} rows={report.orders.value.map(o => [o.poNumber, o.poLine, o.dueDate, inventoryQuantity(o.openQuantity), flag(o.confirmed), o.supplierIdentifier ?? o.supplierDisplay, o.buyerDisplay, o.manufacturerItem, o.trackingInfo])} />)}</Card>
      </div>
      {commentOpen && active && <BuyerCommentDialog part={report.part} text={report.comment.value ?? ''} recordedAt={report.commentRecordedAt} author={report.commentAuthor} onClose={() => setCommentOpen(false)} />}
    </>}
  </div>;
}

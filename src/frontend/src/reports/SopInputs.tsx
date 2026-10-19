import type { SopRequest } from '../api/sopApi';
import { Segmented } from './ReportChrome';

export function SopInputs({ value, onChange }: { value: SopRequest; onChange: (value: SopRequest) => void }) {
  const invalidRange = value.ranges.some(range => {
    const hasFrom = range.from.length > 0;
    const hasTo = range.to.length > 0;
    return hasFrom !== hasTo || (hasFrom && (!/^\d{4}$/.test(range.from) || !/^\d{4}$/.test(range.to)));
  });

  return <div className="sop-inputs">
    <section className="sop-inputs__settings" aria-label="Settings"><h2>Settings</h2><div className="report-fields">
      <label>Site<select value={value.site} onChange={e => onChange({ ...value, site: e.target.value })}>
        {['SW', 'NW', 'AR', 'MN', 'MS', 'KV'].map(v => <option key={v}>{v}</option>)}
      </select></label>
      <Segmented label="Group By" value={value.groupBy} choices={[{ value: 'planr', label: 'Planner ID' }, { value: 'customer', label: 'Customer Name' }]} onChange={groupBy => onChange({ ...value, groupBy })} />
    </div></section>
    <section className="sop-inputs__product-lines" aria-labelledby="sop-product-lines-title">
      <h2 id="sop-product-lines-title">Product lines</h2>
      <div className="sop-inputs__range-header" aria-hidden="true">
        <span>Range</span><span>From</span><span>To</span><span>Remove</span>
      </div>
      <div className="sop-inputs__range-list">
        {value.ranges.map((range, index) => <div className="sop-inputs__range-row" key={index}>
          <span className="sop-inputs__range-name">Range {index + 1}</span>
          <label className="sop-inputs__range-field"><span className="sop-inputs__mobile-label">From</span>
            <input aria-label={`Product Line From ${index + 1}`} aria-invalid={invalidRange && (!/^\d{4}$/.test(range.from) || !range.from) || undefined}
              value={range.from} maxLength={4} inputMode="numeric" onChange={e => onChange({ ...value, ranges: value.ranges.map((r, i) => i === index ? { ...r, from: e.target.value } : r) })} />
          </label>
          <label className="sop-inputs__range-field"><span className="sop-inputs__mobile-label">To</span>
            <input aria-label={`Product Line To ${index + 1}`} aria-invalid={invalidRange && (!/^\d{4}$/.test(range.to) || !range.to) || undefined}
              value={range.to} maxLength={4} inputMode="numeric" onChange={e => onChange({ ...value, ranges: value.ranges.map((r, i) => i === index ? { ...r, to: e.target.value } : r) })} />
          </label>
          <button className="sop-inputs__remove" type="button" disabled={value.ranges.length === 1}
            aria-label={`Remove range ${index + 1}`} onClick={() => onChange({ ...value, ranges: value.ranges.filter((_, i) => i !== index) })}>Remove</button>
        </div>)}
      </div>
      {invalidRange && <p className="sop-inputs__validation" role="alert">Enter both From and To product lines using four digits for each range.</p>}
      <button className="sop-inputs__add" type="button" disabled={value.ranges.length === 5}
        onClick={() => onChange({ ...value, ranges: [...value.ranges, { from: '', to: '' }] })}>Add product line</button>
      <p className="report-hint">Weeks follow the fiscal calendar in Settings.</p>
    </section>
  </div>;
}

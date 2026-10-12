import type { OpenOrderReportRequest } from '../api/openOrderReportApi';
import { oorMandatory, oorOptional } from './openOrderReportPreferences';

export function OpenOrderReportInputs({ value, onChange }: { value: OpenOrderReportRequest; onChange: (value: OpenOrderReportRequest) => void }) {
  const text = (key: keyof Omit<OpenOrderReportRequest, 'optionalFields'>, label: string, digits = false, placeholder?: string, invalid = false) => <label>{label}
    <input value={value[key] ?? ''} maxLength={1024} inputMode={digits ? 'numeric' : undefined} placeholder={placeholder}
      aria-invalid={invalid || undefined} onChange={e => onChange({ ...value, [key]: e.target.value })} /></label>;
  const from = value.productLineFrom?.trim() ?? ''; const to = value.productLineTo?.trim() ?? '';
  const invalidProduct = (!!from !== !!to) || (!!from && (!/^\p{Nd}{4}$/u.test(from) || !/^\p{Nd}{4}$/u.test(to)));
  const invalidDatePair = !!value.dueDateFrom?.trim() !== !!value.dueDateTo?.trim();
  return <div className="oor-inputs">
    <section aria-labelledby="oor-customer-title"><h2 id="oor-customer-title">Customer and site</h2><div className="oor-inputs__row">
      {text('customerName', 'Customer Name', false, 'e.g. Acme')}{text('customerNumber', 'Customer Number')}
      <label>Site<select value={value.site ?? ''} onChange={e => onChange({ ...value, site: e.target.value })}>
        <option value="">All supported sites</option>{['SW', 'NW', 'AR', 'MN', 'MS', 'KV'].map(s => <option key={s}>{s}</option>)}</select></label>
    </div><p className="oor-inputs__hint">Customer Name matches part of a name; use % for any characters and _ for one character. Other text filters match exactly.</p></section>
    <section aria-labelledby="oor-sales-title"><h2 id="oor-sales-title">Sales and product</h2><div className="oor-inputs__row">
      {text('salespersonId', 'Salesperson ID')}{text('ios', 'IOS')}
      <div className="oor-inputs__pair">{text('productLineFrom', 'Product Line From', true, '0000', invalidProduct)}<span aria-hidden="true">to</span>{text('productLineTo', 'Product Line To', true, '0000', invalidProduct)}</div>
    </div>{invalidProduct && <p role="alert">Enter both product-line bounds using four digits, or leave both empty.</p>}</section>
    <section aria-labelledby="oor-dates-title"><h2 id="oor-dates-title">Due dates</h2><div className="oor-inputs__row">
      <div className="oor-inputs__pair">{text('dueDateFrom', 'Due Date From', false, 'MM/DD/YYYY', invalidDatePair)}<span aria-hidden="true">to</span>{text('dueDateTo', 'Due Date To', false, 'MM/DD/YYYY', invalidDatePair)}</div>
    </div><p className="oor-inputs__hint">Both dates or neither. MM/DD/YYYY, MM/DD/YY or MM/DD (current year); . and - separators are supported. End must be on or after start.</p>
      {invalidDatePair && <p role="alert">Enter both due dates or leave both empty.</p>}</section>
    <section aria-labelledby="oor-columns-title"><h2 id="oor-columns-title">Columns</h2>
      <p className="oor-inputs__mandatory"><strong>Always included:</strong> {oorMandatory.join(', ')}.</p>
      <div className="oor-inputs__columns">
        {oorOptional.map(c => <label key={c}><input type="checkbox" checked={value.optionalFields.includes(c)} onChange={e => onChange({ ...value,
          optionalFields: oorOptional.filter(v => v === c ? e.target.checked : value.optionalFields.includes(v)) })} />{c}</label>)}
      </div></section>
    <p className="oor-inputs__hint">Enter at least one non-date filter. An explicit site qualifies; All supported sites or dates alone do not.</p>
  </div>;
}

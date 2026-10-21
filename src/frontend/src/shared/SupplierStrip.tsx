import type { ReactNode } from 'react';
import { ScrollTable } from './partInfoParts';
import './SupplierStrip.css';

/** Display-only supplier section; each consumer owns its acquisition and selected population. */
export function SupplierStrip({ section, isLoading = false, missingIdentity = false, unavailableDetails = false, note, actions }: {
  section: { value: readonly { identifier: string; display: string | null; isCreditHold: boolean | null; isCia: boolean | null }[] | null; error: string | null; acquiredAtUtc: string | null };
  isLoading?: boolean; missingIdentity?: boolean; unavailableDetails?: boolean; note?: string; actions?: ReactNode;
}) {
  const suppliers = section.value;
  const summary = (key: 'isCreditHold' | 'isCia') => {
    if (!suppliers?.length) return 'Unavailable';
    const yes = suppliers.filter(s => s[key] === true).map(s => s.identifier);
    const unknown = suppliers.filter(s => s[key] === null).map(s => s.identifier);
    return `${yes.length ? yes.join(', ') : unknown.length || section.error || missingIdentity ? 'No confirmed Yes' : 'None'}${unknown.length ? ` · ${section.error ? 'Unavailable' : 'Unknown'}: ${unknown.join(', ')}` : ''}${missingIdentity ? ' · Unavailable: supplier identity missing' : ''}`;
  };
  const flag = (value: boolean | null) => value === true ? 'Yes' : value === false ? 'No' : unavailableDetails && section.error ? 'Unavailable' : 'Unknown';
  return <section className="part-info__supplier-strip" aria-label="Supplier Credit Hold / CIA">
    {isLoading ? <p role="status">Loading supplier status…</p> : <>
      {section.error && <p role="alert">{section.error}</p>}
      {suppliers?.length === 0 ? <p>No supplier status is available from open POs.</p> : <div className="part-info__risk-summary"><span><strong>Credit hold:</strong> {summary('isCreditHold')}</span><span><strong>CIA:</strong> {summary('isCia')}</span></div>}
      {missingIdentity && <p className="part-info__hint">Supplier identity is unavailable for one or more qualifying open POs.</p>}
      {!!suppliers?.length && <details><summary>Supplier status details · {suppliers.length} suppliers</summary>
        <ScrollTable label="Supplier status" headings={['Supplier identifier', 'Supplier', 'Credit Hold', 'CIA']} rows={suppliers.map(s => [s.identifier, s.display, flag(s.isCreditHold), flag(s.isCia)])} />
        <p className="part-info__hint">Only suppliers evidenced by qualifying open POs. Unknown is not No.</p></details>}
      {note && <p className="part-info__hint">{note}</p>}
      {actions}
      {section.acquiredAtUtc && <p className="part-info__time">Acquired <time dateTime={section.acquiredAtUtc}>{new Date(section.acquiredAtUtc).toLocaleString()}</time></p>}
    </>}
  </section>;
}

import type { KeyboardEvent, ReactNode } from 'react';
import './partInfoParts.css';

export function PartIdentity({ part, description, chips, actions }: { part: string; description: ReactNode; chips: ReactNode[]; actions?: ReactNode }) {
  return <header className="part-info__identity"><div><h2>{part}</h2><p>{description}</p>{chips}</div>{actions}</header>;
}

export function FactsStrip({ items, label }: { items: [string, ReactNode][]; label?: string }) {
  return <dl className="part-info__facts" aria-label={label}>{items.map(([name, value]) => <div key={name}><dt>{name}</dt><dd>{value}</dd></div>)}</dl>;
}

export function Fields({ values }: { values: [string, ReactNode][] }) {
  return <dl className="part-info__fields">{values.map(([name, value]) => <div key={name}><dt>{name}</dt><dd>{value ?? '—'}</dd></div>)}</dl>;
}

export function InfoCard({ title, children, error, acquired }: { title: string; children: ReactNode; error?: string | null; acquired?: string | null }) {
  return <section className="part-info__card"><h3>{title}</h3>{error && <p role="alert">{error}</p>}{children}
    {acquired && <p className="part-info__time">Acquired <time dateTime={acquired}>{new Date(acquired).toLocaleString()}</time></p>}</section>;
}

export function ScrollTable({ label, headings, rows, numeric = [], className }: { label: string; headings: string[]; rows: ReactNode[][]; numeric?: number[]; className?: string }) {
  return <div className={`part-info__scroll${className ? ` ${className}` : ''}`} tabIndex={0} role="region" aria-label={`Scrollable ${label}`}><table aria-label={label}>
    <thead><tr>{headings.map((h, j) => <th className={numeric.includes(j) ? 'part-info__number' : undefined} key={h} scope="col">{h}</th>)}</tr></thead><tbody>{rows.map((row, i) => <tr key={i}>{row.map((cell, j) => <td className={numeric.includes(j) ? 'part-info__number' : undefined} key={j}>{cell ?? '—'}</td>)}</tr>)}</tbody>
  </table></div>;
}

export function InfoTabs<T extends string>({ idPrefix, tabs, active, onChange, label = 'Part information tables' }: {
  idPrefix: string; tabs: { id: T; label: string; count?: ReactNode; failed?: boolean }[]; active: T; onChange: (id: T) => void; label?: string;
}) {
  function tabKey(event: KeyboardEvent<HTMLButtonElement>, index: number) {
    let next: number;
    if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
    else if (event.key === 'ArrowLeft') next = (index + tabs.length - 1) % tabs.length;
    else if (event.key === 'Home') next = 0;
    else if (event.key === 'End') next = tabs.length - 1;
    else return;
    event.preventDefault(); onChange(tabs[next].id);
    document.getElementById(`${idPrefix}-tab-${tabs[next].id}`)?.focus();
  }
  return <div className="part-info__tabs" role="tablist" aria-label={label}>
    {tabs.map((item, index) => <button key={item.id} id={`${idPrefix}-tab-${item.id}`} role="tab" aria-selected={active === item.id}
      aria-controls={`${idPrefix}-panel-${item.id}`} tabIndex={active === item.id ? 0 : -1} onClick={() => onChange(item.id)} onKeyDown={event => tabKey(event, index)}>
      {item.label} {item.count != null && <span className="part-info__count">{item.count}</span>}{item.failed && <span aria-label="Acquisition failed"> !</span>}
    </button>)}
  </div>;
}

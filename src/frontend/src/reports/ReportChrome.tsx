import { useRef, useState, type ReactNode } from 'react';
import { AnchoredPopover } from '../components/shared/AnchoredPopover';

export function ReportHead({ title, description, children }: { title: string; description?: string; children?: ReactNode }) {
  return <header className="report-head"><div className="report-head__title"><h1>{title}</h1>{description && <span>{description}</span>}</div><div className="report-head__actions">{children}</div></header>;
}
export function ReportInfo({ title, children }: { title: string; children: ReactNode }) {
  const anchor = useRef<HTMLButtonElement>(null); const [open, setOpen] = useState(false);
  return <><button ref={anchor} type="button" className="report-button" aria-label={title} aria-haspopup="dialog" aria-expanded={open} onClick={() => setOpen(v => !v)}>ⓘ</button>
    {open && <AnchoredPopover title={title} anchor={anchor} onClose={() => setOpen(false)}><div className="report-info">{children}</div></AnchoredPopover>}</>;
}
export function Segmented({ label, value, choices, onChange }: { label: string; value: string; choices: readonly { value: string; label: string }[]; onChange: (value: string) => void }) {
  return <div className="report-segment-field"><span>{label}</span><div className="report-segmented" role="group" aria-label={label}>{choices.map(c => <button type="button" key={c.value} aria-pressed={value === c.value} onClick={() => onChange(c.value)}>{c.label}</button>)}</div></div>;
}

import React, { useLayoutEffect, useRef, useState, type ReactNode } from '../../src/frontend/node_modules/react';
import type { LongTermShortageRow } from '../../src/frontend/src/api/longTermShortagesApi';

// Diagnostic fixed-height row virtualization. Deliberately not shipped: keyboard/focus and
// accessibility need independent validation before introducing it into the production table.
export function VirtualBody({ rows, render }: { rows: LongTermShortageRow[]; render: (row: LongTermShortageRow) => ReactNode }) {
  const body = useRef<HTMLTableSectionElement>(null);
  const [range, setRange] = useState({ start: 0, count: 35 });
  useLayoutEffect(() => {
    const grid = body.current!.closest('.long-term-shortages__grid') as HTMLElement;
    const update = () => {
      const height = body.current?.querySelector('tr[data-row]')?.getBoundingClientRect().height ?? 28;
      setRange({ start: Math.max(0, Math.floor(grid.scrollTop / height) - 6), count: Math.ceil(grid.clientHeight / height) + 12 });
    };
    grid.addEventListener('scroll', update);
    const observer = new ResizeObserver(update); observer.observe(grid); update();
    return () => { grid.removeEventListener('scroll', update); observer.disconnect(); };
  }, [rows]);
  const start = Math.min(range.start, Math.max(0, rows.length - range.count));
  const end = Math.min(rows.length, start + range.count);
  return <tbody ref={body}>
    {start > 0 && <tr aria-hidden="true"><td colSpan={33} style={{ height: start * 28, padding: 0 }} /></tr>}
    {rows.slice(start, end).map(render)}
    {end < rows.length && <tr aria-hidden="true"><td colSpan={33} style={{ height: (rows.length - end) * 28, padding: 0 }} /></tr>}
  </tbody>;
}

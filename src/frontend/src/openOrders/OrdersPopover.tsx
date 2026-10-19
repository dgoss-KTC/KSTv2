import { useLayoutEffect, useRef, useState, type ReactNode, type RefObject } from 'react';
import { createPortal } from 'react-dom';

/** Themed overlay outside bounded table scroll. Focus-contained while open; Escape is consumed. */
export function OrdersPopover({ title, anchor, onClose, children, wide = false }: {
  title: string; anchor: RefObject<HTMLElement | null>; onClose: () => void; children: ReactNode; wide?: boolean;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const close = useRef(onClose);
  useLayoutEffect(() => { close.current = onClose; });
  const [position, setPosition] = useState({ top: 0, left: 0 });
  useLayoutEffect(() => {
    const opener = anchor.current;
    const place = () => {
      const rect = opener?.getBoundingClientRect(); const panel = ref.current;
      if (!rect || !panel) return;
      setPosition({ top: Math.max(8, Math.min(rect.bottom + 6, window.innerHeight - panel.offsetHeight - 8)),
        left: Math.max(8, Math.min(rect.left, window.innerWidth - panel.offsetWidth - 8)) });
    };
    const focusable = () => [...(ref.current?.querySelectorAll<HTMLElement>('button:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex="0"]') ?? [])];
    place(); (focusable()[0] ?? ref.current)?.focus();
    const pointer = (event: PointerEvent) => { if (!ref.current?.contains(event.target as Node) && !opener?.contains(event.target as Node)) close.current(); };
    const key = (event: KeyboardEvent) => {
      if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); close.current(); }
      if (event.key !== 'Tab') return;
      const items = focusable(); const first = items[0]; const last = items[items.length - 1];
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
    };
    document.addEventListener('pointerdown', pointer); document.addEventListener('keydown', key, true);
    window.addEventListener('resize', place); window.addEventListener('scroll', place, true);
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(place); if (ref.current) observer?.observe(ref.current);
    return () => {
      observer?.disconnect(); document.removeEventListener('pointerdown', pointer); document.removeEventListener('keydown', key, true);
      window.removeEventListener('resize', place); window.removeEventListener('scroll', place, true);
      if (opener?.isConnected) opener.focus({ preventScroll: true });
    };
  }, [anchor]);
  return createPortal(<div ref={ref} role="dialog" aria-label={title} tabIndex={-1} className={`orders-popover${wide ? ' orders-popover--wide' : ''}`} style={position}>
    <header><strong>{title}</strong><button type="button" aria-label={`Close ${title}`} onClick={onClose}>×</button></header>{children}
  </div>, document.querySelector('.shell') ?? document.body);
}

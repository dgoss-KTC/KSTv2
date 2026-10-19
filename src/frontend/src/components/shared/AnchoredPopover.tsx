import { useLayoutEffect, useRef, useState, type ReactNode, type RefObject } from 'react';
import { createPortal } from 'react-dom';
import './AnchoredPopover.css';

/** Small themed, focus-contained overlay. Portal is a shell sibling of the scroll frame, not its child. */
export function AnchoredPopover({ title, anchor, onClose, children }: {
  title: string; anchor: RefObject<HTMLElement | null>; onClose: () => void; children: ReactNode;
}) {
  const panel = useRef<HTMLDivElement>(null);
  const host = document.querySelector('.shell') ?? document.body;
  const close = useRef(onClose);
  useLayoutEffect(() => { close.current = onClose; });
  const [position, setPosition] = useState({ top: 8, left: 8 });
  useLayoutEffect(() => {
    const opener = anchor.current;
    const place = () => {
      const rect = opener?.getBoundingClientRect(); const element = panel.current;
      if (!rect || !element) return;
      const below = rect.bottom + 6;
      const top = below + element.offsetHeight <= window.innerHeight - 8 ? below : rect.top - element.offsetHeight - 6;
      setPosition({ top: Math.max(8, Math.min(top, window.innerHeight - element.offsetHeight - 8)),
        left: Math.max(8, Math.min(rect.left, window.innerWidth - element.offsetWidth - 8)) });
    };
    const items = () => [...(panel.current?.querySelectorAll<HTMLElement>('button:not([disabled]),input:not([disabled]),select:not([disabled]),[tabindex="0"]') ?? [])];
    place(); (items()[0] ?? panel.current)?.focus();
    const outside = (event: PointerEvent) => { if (!panel.current?.contains(event.target as Node) && !opener?.contains(event.target as Node)) close.current(); };
    const key = (event: KeyboardEvent) => {
      if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); close.current(); }
      if (event.key !== 'Tab') return;
      const focus = items(); const first = focus[0]; const last = focus[focus.length - 1];
      if (!first) { event.preventDefault(); panel.current?.focus(); }
      else if (event.shiftKey && (document.activeElement === first || document.activeElement === panel.current)) { event.preventDefault(); last.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    };
    document.addEventListener('pointerdown', outside); document.addEventListener('keydown', key, true);
    window.addEventListener('resize', place); window.addEventListener('scroll', place, true);
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(place);
    if (panel.current) observer?.observe(panel.current);
    return () => {
      observer?.disconnect(); document.removeEventListener('pointerdown', outside); document.removeEventListener('keydown', key, true);
      window.removeEventListener('resize', place); window.removeEventListener('scroll', place, true);
      if (opener?.isConnected) opener.focus({ preventScroll: true });
    };
  }, [anchor, host]);
  return createPortal(<div ref={panel} className="anchored-popover" style={position} role="dialog" aria-label={title} tabIndex={-1}>
    <header><strong>{title}</strong><button type="button" aria-label={`Close ${title}`} onClick={onClose}>×</button></header>
    {children}
  </div>, host);
}

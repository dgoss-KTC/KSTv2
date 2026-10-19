import { useCallback, useLayoutEffect, useRef, useState, type PointerEvent as ReactPointerEvent } from 'react';
import type { PlanColumnId } from './planLayout';

export type ColumnDragFeedback = { source: PlanColumnId; target: PlanColumnId | null; x: number; y: number; host: HTMLElement };
type Gesture = { cancel: () => void };

// Windows Tauri's native file-drop handler intercepts HTML5 drag/drop. Keep that host setting
// unchanged and use pointer events only for local, registry-validated column moves.
export function useColumnDrag(ids: readonly PlanColumnId[], context: string,
  move: (source: PlanColumnId, target: PlanColumnId) => void, axis: 'x' | 'y') {
  const [feedback, setFeedback] = useState<ColumnDragFeedback | null>(null);
  const gesture = useRef<Gesture | null>(null);
  const moveRef = useRef(move);
  useLayoutEffect(() => { moveRef.current = move; });
  const cancel = useCallback(() => { gesture.current?.cancel(); }, []);
  useLayoutEffect(() => cancel, [context, cancel]);

  function start(event: ReactPointerEvent<HTMLElement>, source: PlanColumnId) {
    if (event.button !== 0 || event.isPrimary === false || !ids.includes(source) ||
      (event.target as Element).closest('button,input,select,textarea,a')) return;
    cancel();
    const element = event.currentTarget;
    const surface = element.closest<HTMLElement>('[data-column-drag-surface]');
    if (!surface) return;
    event.preventDefault(); element.focus({ preventScroll: true });
    const doc = element.ownerDocument; const win = doc.defaultView!;
    const host = element.closest<HTMLElement>('.shell') ?? doc.body;
    const pointerId = event.pointerId; const originX = event.clientX; const originY = event.clientY;
    let x = originX; let y = originY; let active = false; let frame = 0;
    let target: PlanColumnId | null = null;
    function hit() {
      const row = doc.elementFromPoint(x, y)?.closest<HTMLElement>('[data-order-column]');
      const id = row?.dataset.orderColumn as PlanColumnId | undefined;
      return row && surface!.contains(row) && id && ids.includes(id) ? id : null;
    }
    function preview() {
      target = hit();
      setFeedback(previous => previous?.target === target && previous.x === x && previous.y === y
        ? previous : { source, target, x, y, host });
    }
    function scroll() {
      const rect = surface!.getBoundingClientRect();
      if (x >= rect.left - 24 && x <= rect.right + 24 && y >= rect.top - 24 && y <= rect.bottom + 24) {
        if (axis === 'x') surface!.scrollLeft += x < rect.left + 40 ? -24 : x > rect.right - 40 ? 24 : 0;
        else surface!.scrollTop += y < rect.top + 32 ? -20 : y > rect.bottom - 32 ? 20 : 0;
      }
      preview(); frame = win.requestAnimationFrame(scroll);
    }
    function onMove(e: PointerEvent) {
      if (e.pointerId !== pointerId) return;
      x = e.clientX; y = e.clientY;
      if (!active && Math.hypot(x - originX, y - originY) < 5) return;
      e.preventDefault();
      if (!active) { active = true; doc.documentElement.classList.add('orders-column-dragging'); frame = win.requestAnimationFrame(scroll); }
      preview();
    }
    function stop() {
      doc.removeEventListener('pointermove', onMove); doc.removeEventListener('pointerup', finish);
      doc.removeEventListener('pointercancel', onCancel); win.removeEventListener('keydown', key, true);
      win.removeEventListener('blur', stop); element.removeEventListener('lostpointercapture', stop);
      win.cancelAnimationFrame(frame); doc.documentElement.classList.remove('orders-column-dragging');
      gesture.current = null; setFeedback(null);
      if (element.hasPointerCapture?.(pointerId)) element.releasePointerCapture(pointerId);
    }
    function finish(e: PointerEvent) {
      if (e.pointerId !== pointerId) return;
      x = e.clientX; y = e.clientY; const destination = active ? hit() : null;
      stop(); if (destination && destination !== source) moveRef.current(source, destination);
    }
    function onCancel(e: PointerEvent) { if (e.pointerId === pointerId) stop(); }
    function key(e: KeyboardEvent) {
      if (e.key !== 'Escape') return;
      e.preventDefault(); e.stopPropagation(); stop();
    }
    gesture.current = { cancel: stop };
    doc.addEventListener('pointermove', onMove, { passive: false }); doc.addEventListener('pointerup', finish);
    doc.addEventListener('pointercancel', onCancel); win.addEventListener('keydown', key, true);
    win.addEventListener('blur', stop); element.addEventListener('lostpointercapture', stop);
    element.setPointerCapture?.(pointerId);
  }
  return { start, feedback };
}


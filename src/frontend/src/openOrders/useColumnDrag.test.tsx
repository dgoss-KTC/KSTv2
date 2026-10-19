import { act, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { useColumnDrag } from './useColumnDrag';
import { ColumnDragPreview } from './ColumnDragPreview';
import { columnPointer } from './pointerTestSupport';

const originalHitTest = Object.getOwnPropertyDescriptor(document, 'elementFromPoint');
const move = vi.fn(); let hit: Element | null; let tick: FrameRequestCallback;
beforeEach(() => {
  move.mockReset(); hit = null;
  Object.defineProperty(document, 'elementFromPoint', { configurable: true, value: () => hit });
  vi.spyOn(window, 'requestAnimationFrame').mockImplementation(callback => { tick = callback; return 1; });
  vi.spyOn(window, 'cancelAnimationFrame').mockImplementation(() => {});
});
afterEach(() => {
  vi.restoreAllMocks();
  if (originalHitTest) Object.defineProperty(document, 'elementFromPoint', originalHitTest);
  else Reflect.deleteProperty(document, 'elementFromPoint');
});
function UI({ context = 'A', axis = 'x' }: { context?: string; axis?: 'x' | 'y' }) {
  const drag = useColumnDrag(['order', 'po'], context, move, axis);
  return <div className="shell"><ColumnDragPreview feedback={drag.feedback} />
    <div data-column-drag-surface="test" data-testid="surface">
      <div data-order-column="order" tabIndex={0} onPointerDown={e => drag.start(e, 'order')}>
        SO<button>Nested action</button></div>
      <div data-order-column="po">PO</div>
    </div><div data-order-column="po" data-testid="outside">Outside same-named column</div></div>;
}
function start() {
  const source = screen.getByText('SO'); const target = screen.getByText('PO');
  const capture = vi.fn(); Object.defineProperty(source, 'setPointerCapture', { value: capture, configurable: true });
  columnPointer('pointerdown', source, 100, 100); hit = target;
  columnPointer('pointermove', source, 150, 100); // capture retargets to source; hit-test must use coordinates
  return { source, target, capture };
}
it('uses pointer capture, coordinate hit testing and one move on release; no HTML5 drag payload is required', () => {
  render(<UI />); const { source, capture } = start();
  expect(capture).toHaveBeenCalledWith(1); expect(source).toHaveFocus();
  expect(document.querySelector('.orders-column-drag-preview')).toHaveTextContent('SO');
  expect(move).not.toHaveBeenCalled();
  columnPointer('pointerup', document, 150, 100);
  expect(move).toHaveBeenCalledExactlyOnceWith('order', 'po');
  expect(document.querySelector('.orders-column-drag-preview')).toBeNull();
  expect(document.documentElement).not.toHaveClass('orders-column-dragging');
  columnPointer('pointerup', document, 150, 100); expect(move).toHaveBeenCalledTimes(1);
});
it('does not turn clicks, right-clicks, controls or a different pointer into moves', () => {
  render(<UI />); const source = screen.getByText('SO'); hit = screen.getByText('PO');
  columnPointer('pointerdown', source, 100, 100); columnPointer('pointermove', document, 102, 100); columnPointer('pointerup', document, 102, 100);
  columnPointer('pointerdown', source, 100, 100, 1, 2); columnPointer('pointermove', document, 150, 100); columnPointer('pointerup', document, 150, 100);
  columnPointer('pointerdown', screen.getByRole('button'), 100, 100); columnPointer('pointermove', document, 150, 100); columnPointer('pointerup', document, 150, 100);
  columnPointer('pointerdown', source, 100, 100); columnPointer('pointermove', document, 150, 100, 2); columnPointer('pointerup', document, 150, 100, 2);
  columnPointer('pointerup', document, 100, 100);
  expect(move).not.toHaveBeenCalled();
});
it.each(['outside', 'unknown', 'self', 'none'])('refuses drops outside its own validated column surface: %s', destination => {
  render(<UI />); const { source } = start();
  const unknown = document.createElement('div'); unknown.dataset.orderColumn = 'unknown'; screen.getByTestId('surface').append(unknown);
  hit = destination === 'outside' ? screen.getByTestId('outside') : destination === 'self' ? source : destination === 'unknown' ? unknown : null;
  columnPointer('pointerup', document, 150, 100); expect(move).not.toHaveBeenCalled();
});
it.each(['Escape', 'pointercancel', 'blur', 'lostpointercapture', 'context', 'unmount'])('cancels without committing and cleans up on %s', kind => {
  const view = render(<UI />); const { source } = start();
  if (kind === 'Escape') fireEvent.keyDown(source, { key: 'Escape' });
  else if (kind === 'pointercancel') columnPointer('pointercancel', document, 150, 100);
  else if (kind === 'blur') fireEvent(window, new Event('blur'));
  else if (kind === 'lostpointercapture') fireEvent(source, new Event('lostpointercapture'));
  else if (kind === 'context') view.rerender(<UI context="B" />);
  else view.unmount();
  columnPointer('pointerup', document, 150, 100); expect(move).not.toHaveBeenCalled();
  expect(document.documentElement).not.toHaveClass('orders-column-dragging');
  expect(document.querySelector('.orders-column-drag-preview')).toBeNull();
  expect(window.cancelAnimationFrame).toHaveBeenCalled();
});
it.each(['x', 'y'] as const)('continues edge scrolling while the pointer is held still on axis %s', axis => {
  render(<UI axis={axis} />); const surface = screen.getByTestId('surface');
  vi.spyOn(surface, 'getBoundingClientRect').mockReturnValue({ left: 0, right: 160, top: 0, bottom: 120 } as DOMRect);
  start();
  // The next animation tick uses the retained position without requiring another pointermove.
  act(() => tick(0));
  expect(axis === 'x' ? surface.scrollLeft : surface.scrollTop).toBeGreaterThan(0);
  columnPointer('pointercancel', document, 150, 100);
});

import { createPortal } from 'react-dom';
import type { ColumnDragFeedback } from './useColumnDrag';
import { columnLabel } from './views';

export function ColumnDragPreview({ feedback }: { feedback: ColumnDragFeedback | null }) {
  return feedback && createPortal(<div className="orders-column-drag-preview" aria-hidden="true"
    style={{ left: Math.max(8, Math.min(feedback.x + 12, window.innerWidth - 210)), top: Math.max(8, Math.min(feedback.y + 12, window.innerHeight - 44)) }}>
    ⠿ {columnLabel(feedback.source)}
  </div>, feedback.host);
}

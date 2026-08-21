import { useCallback, useEffect } from 'react';
import type { WorkspaceAssignmentDto } from '../api/client';
import './ManageWorkspacesDialog.css';

interface ManageWorkspacesDialogProps {
  activeWorkspaces: WorkspaceAssignmentDto[];
  archivedWorkspaces: WorkspaceAssignmentDto[];
  onRestore: (workspace: WorkspaceAssignmentDto) => void;
  onDelete: (workspace: WorkspaceAssignmentDto, triggerEl: HTMLElement) => void;
  onResetRequest: (triggerEl: HTMLElement) => void;
  onClose: () => void;
  // Lifted so a caller can restore focus into this dialog (e.g. after a stacked ConfirmDialog
  // closes and its original trigger is no longer connected) without this component exposing an
  // imperative handle.
  containerRef: React.RefObject<HTMLDivElement | null>;
}

function describeScope(w: WorkspaceAssignmentDto): string {
  const parts: string[] = [];
  if (w.productLineFrom) {
    parts.push(
      w.productLineTo && w.productLineTo !== w.productLineFrom
        ? `PL ${w.productLineFrom}\u2013${w.productLineTo}`
        : `PL ${w.productLineFrom}`,
    );
  }
  if (w.parentParts && w.parentParts.length > 0) {
    parts.push(`${w.parentParts.length} ${w.parentParts.length === 1 ? 'part' : 'parts'}`);
  }
  return parts.join(' \u00b7 ') || '\u2014';
}

export function ManageWorkspacesDialog({
  activeWorkspaces,
  archivedWorkspaces,
  onRestore,
  onDelete,
  onResetRequest,
  onClose,
  containerRef,
}: ManageWorkspacesDialogProps) {
  const dialogRef = containerRef;

  useEffect(() => {
    dialogRef.current?.focus();
  }, [dialogRef]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLDivElement>) => {
    if (e.key !== 'Tab') return;
    const focusable = dialogRef.current?.querySelectorAll<HTMLElement>('button:not([disabled])');
    if (!focusable || focusable.length === 0) return;
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (e.shiftKey && document.activeElement === first) {
      e.preventDefault();
      last.focus();
    } else if (!e.shiftKey && document.activeElement === last) {
      e.preventDefault();
      first.focus();
    }
  }, [dialogRef]);

  return (
    <div className="manage-dialog-backdrop" onClick={onClose}>
      <div
        ref={dialogRef}
        className="manage-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="manage-dialog-title"
        tabIndex={-1}
        onClick={(e) => e.stopPropagation()}
        onKeyDown={handleKeyDown}
      >
        <h2 id="manage-dialog-title" className="manage-dialog__title">
          Manage Workspaces
        </h2>

        <section className="manage-dialog__section">
          <h3 className="manage-dialog__section-title">Active</h3>
          {activeWorkspaces.length === 0 ? (
            <p className="manage-dialog__empty">No active workspaces.</p>
          ) : (
            <ul className="manage-dialog__list">
              {activeWorkspaces.map((w) => (
                <li key={w.assignmentId} className="manage-dialog__item">
                  <div className="manage-dialog__item-text">
                    <div className="manage-dialog__item-name">{w.displayName ?? w.site}</div>
                    <div className="manage-dialog__item-details">
                      {w.site} &middot; {describeScope(w)}
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="manage-dialog__section">
          <h3 className="manage-dialog__section-title">Archived</h3>
          {archivedWorkspaces.length === 0 ? (
            <p className="manage-dialog__empty">No archived workspaces.</p>
          ) : (
            <ul className="manage-dialog__list">
              {archivedWorkspaces.map((w) => (
                <li key={w.assignmentId} className="manage-dialog__item">
                  <div className="manage-dialog__item-text">
                    <div className="manage-dialog__item-name">{w.displayName ?? w.site}</div>
                    <div className="manage-dialog__item-details">
                      {w.site} &middot; {describeScope(w)}
                    </div>
                  </div>
                  <div className="manage-dialog__item-actions">
                    <button
                      type="button"
                      className="manage-dialog__btn manage-dialog__btn--secondary"
                      onClick={() => onRestore(w)}
                    >
                      Restore
                    </button>
                    <button
                      type="button"
                      className="manage-dialog__btn manage-dialog__btn--destructive"
                      onClick={(e) => onDelete(w, e.currentTarget)}
                    >
                      Delete Permanently
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>

        <div className="manage-dialog__footer">
          <button
            type="button"
            className="manage-dialog__btn manage-dialog__btn--destructive-outline"
            onClick={(e) => onResetRequest(e.currentTarget)}
          >
            Reset Workspace Configuration
          </button>
          <button type="button" className="manage-dialog__btn manage-dialog__btn--secondary" onClick={onClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  );
}

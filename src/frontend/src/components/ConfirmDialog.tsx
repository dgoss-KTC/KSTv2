import { useCallback, useEffect, useRef, useState } from 'react';
import './ConfirmDialog.css';

interface ConfirmDialogProps {
  title: string;
  body: string;
  cancelLabel?: string;
  confirmLabel: string;
  destructive?: boolean;
  onConfirm: () => void | Promise<void>;
  onCancel: () => void;
  // Reports busy transitions to the caller (ApplicationShell), which owns the single Escape
  // listener that arbitrates across all stacked workspace dialogs and needs to know synchronously
  // whether Cancel is currently allowed here.
  onBusyChange?: (busy: boolean) => void;
}

export function ConfirmDialog({
  title,
  body,
  cancelLabel = 'Cancel',
  confirmLabel,
  destructive = false,
  onConfirm,
  onCancel,
  onBusyChange,
}: ConfirmDialogProps) {
  const [busy, setBusy] = useState(false);
  const dialogRef = useRef<HTMLDivElement>(null);
  const cancelRef = useRef<HTMLButtonElement>(null);
  const confirmRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    // Initial focus lands on the safe action when destructive, otherwise on confirm.
    (destructive ? cancelRef.current : confirmRef.current)?.focus();
  }, [destructive]);

  const setBusyState = (value: boolean) => {
    setBusy(value);
    onBusyChange?.(value);
  };

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
  }, []);

  const handleConfirm = async () => {
    setBusyState(true);
    try {
      await onConfirm();
    } finally {
      setBusyState(false);
    }
  };

  return (
    <div className="confirm-dialog-backdrop" onClick={() => !busy && onCancel()}>
      <div
        ref={dialogRef}
        className="confirm-dialog"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="confirm-dialog-title"
        aria-describedby="confirm-dialog-body"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={handleKeyDown}
      >
        <h2 id="confirm-dialog-title" className="confirm-dialog__title">
          {title}
        </h2>
        <p id="confirm-dialog-body" className="confirm-dialog__body">
          {body}
        </p>
        <div className="confirm-dialog__actions">
          <button
            ref={cancelRef}
            type="button"
            className="confirm-dialog__btn confirm-dialog__btn--secondary"
            onClick={onCancel}
            disabled={busy}
          >
            {cancelLabel}
          </button>
          <button
            ref={confirmRef}
            type="button"
            className={`confirm-dialog__btn ${destructive ? 'confirm-dialog__btn--destructive' : 'confirm-dialog__btn--primary'}`}
            onClick={handleConfirm}
            disabled={busy}
            aria-busy={busy}
          >
            {busy ? '\u2026' : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}

import { useEffect, useLayoutEffect, useRef, type ReactNode } from 'react';

const FOCUSABLE = 'button:not([disabled]), input:not([disabled]), [href], select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

/** Focus the enabled `data-autofocus` control, else the first focusable control, else the dialog itself. */
function focusInside(dialog: HTMLElement | null) {
  if (!dialog) return;
  const target = dialog.querySelector<HTMLElement>('[data-autofocus]:not([disabled])') ?? dialog.querySelector<HTMLElement>(FOCUSABLE);
  (target ?? dialog).focus();
}

/**
 * Modal dialog for the Planning module: focus moves into the dialog on open (the element marked
 * `data-autofocus`, otherwise the first focusable control), Tab is kept inside, Escape closes, and
 * on close focus returns to `returnFocus()` when given (resolved at close time, for openers that are
 * replaced or removed while the dialog is open), otherwise to the element focused when it opened.
 * When the focused control is removed or disabled while the dialog stays open (for example the action
 * buttons change between steps), focus moves to the enabled `data-autofocus` control, else the first
 * focusable control, else the dialog itself, so keyboard users never fall out of the dialog. A caller
 * whose dialog moves between steps passes `focusKey`; when it changes, focus moves to the new step's
 * data-autofocus control (a step with none moves it to the first enabled control).
 */

export function PlanningDialog({ titleId, title, tag, onClose, children, actions, wide, returnFocus, focusKey }: {
  titleId: string;
  title: string;
  tag?: ReactNode;
  onClose: () => void;
  children: ReactNode;
  actions: ReactNode;
  wide?: boolean;
  returnFocus?: () => HTMLElement | null;
  focusKey?: string;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const closeRef = useRef(onClose);
  useEffect(() => { closeRef.current = onClose; }, [onClose]);
  const returnRef = useRef(returnFocus);
  useEffect(() => { returnRef.current = returnFocus; }, [returnFocus]);

  useEffect(() => {
    const opener = document.activeElement instanceof HTMLElement && document.activeElement !== document.body ? document.activeElement : null;
    const dialog = ref.current;
    focusInside(dialog);
    return () => {
      const explicit = returnRef.current?.() ?? null;
      const back = explicit?.isConnected ? explicit : opener?.isConnected ? opener : null;
      back?.focus();
    };
  }, []);

  // The dialog moved to another step: focus its primary control.
  const firstKey = useRef(focusKey);
  useLayoutEffect(() => {
    if (focusKey === firstKey.current) return;
    firstKey.current = focusKey;
    focusInside(ref.current);
  }, [focusKey]);

  // After every render: if focus was lost from inside the dialog (control removed or disabled), recover it.
  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    const active = document.activeElement;
    const lost = active === null || active === document.body || (dialog.contains(active) && active instanceof HTMLElement
      && (active as HTMLButtonElement).disabled === true);
    if (lost) focusInside(dialog);
  });

  const onKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {
    if (e.key === 'Escape') {
      e.stopPropagation();
      closeRef.current();
      return;
    }
    if (e.key !== 'Tab') return;
    const focusable = ref.current ? [...ref.current.querySelectorAll<HTMLElement>(FOCUSABLE)] : [];
    if (focusable.length === 0) return;
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
    else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
  };

  return (
    <div className="pwb-dialog-backdrop">
      <div ref={ref} className={`pwb-dialog${wide ? ' pwb-dialog--wide' : ''}`} role="dialog" aria-modal="true" aria-labelledby={titleId} tabIndex={-1} onKeyDown={onKeyDown}>
        <header className="pwb-dialog__header">
          <h2 id={titleId} className="pwb-dialog__title">{title}</h2>
          {tag}
          <button type="button" className="pwb-icon-btn pwb-dialog__close" aria-label="Close" onClick={onClose}>{'\u00d7'}</button>
        </header>
        <div className="pwb-dialog__body">{children}</div>
        <footer className="pwb-dialog__actions">{actions}</footer>
      </div>
    </div>
  );
}

import { useState, useEffect, useRef, useCallback } from 'react';
import type { WorkspaceAssignmentDto } from '../api/client';
import type { CreateWorkspaceFields, WorkspaceApiError, WorkspaceValidationErrors } from '../api/workspaceApi';
import './AddWorkspaceDialog.css';

interface AddWorkspaceDialogProps {
  workspace?: WorkspaceAssignmentDto;
  onSave: (fields: CreateWorkspaceFields) => Promise<void>;
  onClose: () => void;
}

interface FormState {
  displayName: string;
  site: string;
  customerNumber: string;
  productLineFrom: string;
  productLineTo: string;
  isTemporary: boolean;
  coverageEndsOn: string;
}

const INITIAL_FORM: FormState = {
  displayName: '',
  site: '',
  customerNumber: '',
  productLineFrom: '',
  productLineTo: '',
  isTemporary: false,
  coverageEndsOn: '',
};

function formFromWorkspace(workspace?: WorkspaceAssignmentDto): FormState {
  if (!workspace) return INITIAL_FORM;
  return {
    displayName: workspace.displayName ?? '',
    site: workspace.site ?? '',
    customerNumber: workspace.customerNumber ?? '',
    productLineFrom: workspace.productLineFrom ?? '',
    productLineTo: workspace.productLineTo ?? '',
    isTemporary: workspace.isTemporary,
    coverageEndsOn: workspace.coverageEndsOn ?? '',
  };
}

function hasMinimumScope(form: FormState): boolean {
  const site = form.site.trim();
  if (site.length !== 2 || !/^[A-Za-z]{2}$/.test(site)) return false;
  return form.customerNumber.trim().length > 0 || form.productLineFrom.trim().length > 0;
}

export function AddWorkspaceDialog({ workspace, onSave, onClose }: AddWorkspaceDialogProps) {
  const isEditMode = workspace != null;
  const [form, setForm] = useState<FormState>(() => formFromWorkspace(workspace));
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<WorkspaceValidationErrors>({});
  const dialogRef = useRef<HTMLDivElement>(null);
  const firstInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    firstInputRef.current?.focus();
  }, []);

  // Trap focus inside dialog
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLDivElement>) => {
      if (e.key === 'Escape' && !saving) {
        onClose();
        return;
      }
      if (e.key !== 'Tab') return;
      const focusable = dialogRef.current?.querySelectorAll<HTMLElement>(
        'button:not([disabled]), input:not([disabled]), [tabindex]:not([tabindex="-1"])',
      );
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
    },
    [saving, onClose],
  );

  const setField = (name: keyof FormState) => (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.type === 'checkbox' ? e.target.checked : e.target.value;
    setForm((prev) => ({ ...prev, [name]: value }));
    if (fieldErrors[name]) {
      setFieldErrors((prev) => {
        const next = { ...prev };
        delete next[name];
        return next;
      });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!hasMinimumScope(form) || saving) return;

    setSaving(true);
    setFieldErrors({});

    try {
      await onSave({
        displayName: form.displayName.trim() || undefined,
        site: form.site.trim(),
        customerNumber: form.customerNumber.trim() || undefined,
        productLineFrom: form.productLineFrom.trim() || undefined,
        productLineTo: form.productLineTo.trim() || undefined,
        isTemporary: form.isTemporary,
        coverageEndsOn: form.isTemporary && form.coverageEndsOn ? form.coverageEndsOn : null,
      });
    } catch (err) {
      setSaving(false);
      if (isWorkspaceApiError(err)) {
        setFieldErrors(err.errors);
      }
    }
  };

  const fieldError = (name: string): string | undefined =>
    fieldErrors[name]?.[0] ?? fieldErrors['scope']?.[0];

  return (
    <div className="dialog-backdrop" onClick={() => !saving && onClose()}>
      <div
        ref={dialogRef}
        className="dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="dialog-title"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={handleKeyDown}
      >
        <h2 id="dialog-title" className="dialog__title">
          {isEditMode ? 'Edit Workspace' : 'Add Workspace'}
        </h2>

        <form className="dialog__form" onSubmit={handleSubmit} noValidate>
          <div className="dialog__helper-text">
            Enter a customer number, a product-line range, or both. All entered filters will be
            applied together.
          </div>

          <div className="dialog__field">
            <label htmlFor="dlg-site" className="dialog__label">
              Site <span aria-hidden="true">*</span>
            </label>
            <input
              ref={firstInputRef}
              id="dlg-site"
              type="text"
              className={`dialog__input${fieldError('site') ? ' dialog__input--error' : ''}`}
              value={form.site}
              onChange={setField('site')}
              maxLength={2}
              placeholder="NW"
              autoComplete="off"
              aria-describedby={fieldError('site') ? 'dlg-site-err' : undefined}
              aria-required="true"
            />
            {fieldError('site') && (
              <span id="dlg-site-err" className="dialog__field-error" role="alert">
                {fieldError('site')}
              </span>
            )}
          </div>

          <div className="dialog__field">
            <label htmlFor="dlg-customer" className="dialog__label">
              Customer number
            </label>
            <input
              id="dlg-customer"
              type="text"
              className={`dialog__input${fieldError('customerNumber') ? ' dialog__input--error' : ''}`}
              value={form.customerNumber}
              onChange={setField('customerNumber')}
              maxLength={8}
              placeholder="12345678"
              autoComplete="off"
              aria-describedby={fieldError('customerNumber') ? 'dlg-customer-err' : undefined}
            />
            {fieldError('customerNumber') && (
              <span id="dlg-customer-err" className="dialog__field-error" role="alert">
                {fieldError('customerNumber')}
              </span>
            )}
          </div>

          <div className="dialog__row">
            <div className="dialog__field">
              <label htmlFor="dlg-pl-from" className="dialog__label">
                Product Line From
              </label>
              <input
                id="dlg-pl-from"
                type="text"
                className={`dialog__input${fieldError('productLineFrom') ? ' dialog__input--error' : ''}`}
                value={form.productLineFrom}
                onChange={setField('productLineFrom')}
                maxLength={4}
                placeholder="0040"
                autoComplete="off"
                aria-describedby={fieldError('productLineFrom') ? 'dlg-pl-from-err' : undefined}
              />
              {fieldError('productLineFrom') && (
                <span id="dlg-pl-from-err" className="dialog__field-error" role="alert">
                  {fieldError('productLineFrom')}
                </span>
              )}
            </div>

            <div className="dialog__field">
              <label htmlFor="dlg-pl-to" className="dialog__label">
                Product Line To
              </label>
              <input
                id="dlg-pl-to"
                type="text"
                className={`dialog__input${fieldError('productLineTo') ? ' dialog__input--error' : ''}`}
                value={form.productLineTo}
                onChange={setField('productLineTo')}
                maxLength={4}
                placeholder="0045"
                autoComplete="off"
                disabled={!form.productLineFrom.trim()}
                aria-describedby={fieldError('productLineTo') ? 'dlg-pl-to-err' : undefined}
              />
              {fieldError('productLineTo') && (
                <span id="dlg-pl-to-err" className="dialog__field-error" role="alert">
                  {fieldError('productLineTo')}
                </span>
              )}
            </div>
          </div>

          {fieldErrors['scope'] && !fieldError('site') && !fieldError('customerNumber') && (
            <div className="dialog__field-error dialog__scope-error" role="alert">
              {fieldErrors['scope'][0]}
            </div>
          )}

          <div className="dialog__field">
            <label htmlFor="dlg-name" className="dialog__label">
              Tab name
            </label>
            <input
              id="dlg-name"
              type="text"
              className="dialog__input"
              value={form.displayName}
              onChange={setField('displayName')}
              placeholder="Optional friendly label"
              autoComplete="off"
            />
          </div>

          <div className="dialog__field dialog__field--checkbox">
            <input
              id="dlg-temporary"
              type="checkbox"
              checked={form.isTemporary}
              onChange={setField('isTemporary')}
            />
            <label htmlFor="dlg-temporary" className="dialog__label dialog__label--inline">
              Temporary coverage
            </label>
          </div>

          {form.isTemporary && (
            <div className="dialog__field">
              <label htmlFor="dlg-coverage-ends" className="dialog__label">
                Coverage end date
              </label>
              <input
                id="dlg-coverage-ends"
                type="date"
                className="dialog__input"
                value={form.coverageEndsOn}
                onChange={setField('coverageEndsOn')}
              />
            </div>
          )}

          <div className="dialog__actions">
            <button
              type="button"
              className="dialog__btn dialog__btn--secondary"
              onClick={onClose}
              disabled={saving}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="dialog__btn dialog__btn--primary"
              disabled={!hasMinimumScope(form) || saving}
              aria-busy={saving}
            >
              {isEditMode
                ? saving
                  ? 'Saving\u2026'
                  : 'Save Changes'
                : saving
                  ? 'Adding\u2026'
                  : 'Add Workspace'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function isWorkspaceApiError(err: unknown): err is Extract<WorkspaceApiError, { type: 'validation' }> {
  return (
    typeof err === 'object' &&
    err !== null &&
    'type' in err &&
    (err as WorkspaceApiError).type === 'validation'
  );
}

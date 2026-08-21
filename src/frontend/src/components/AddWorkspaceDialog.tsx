import { useState, useEffect, useRef, useCallback } from 'react';
import type { WorkspaceAssignmentDto } from '../api/client';
import type { CreateWorkspaceFields, WorkspaceApiError, WorkspaceValidationErrors } from '../api/workspaceApi';
import './AddWorkspaceDialog.css';

interface AddWorkspaceDialogProps {
  workspace?: WorkspaceAssignmentDto;
  onSave: (fields: CreateWorkspaceFields) => Promise<void>;
  onClose: () => void;
  // Reports saving transitions to the caller (ApplicationShell), which owns the single Escape
  // listener that arbitrates across all stacked workspace dialogs and needs to know synchronously
  // whether closing is currently allowed here.
  onSavingChange?: (saving: boolean) => void;
}

interface FormState {
  displayName: string;
  site: string;
  productLineFrom: string;
  productLineTo: string;
  parentParts: string[];
  isTemporary: boolean;
  coverageEndsOn: string;
}

const INITIAL_FORM: FormState = {
  displayName: '',
  site: '',
  productLineFrom: '',
  productLineTo: '',
  parentParts: [],
  isTemporary: false,
  coverageEndsOn: '',
};

function formFromWorkspace(workspace?: WorkspaceAssignmentDto): FormState {
  if (!workspace) return INITIAL_FORM;
  return {
    displayName: workspace.displayName ?? '',
    site: workspace.site ?? '',
    productLineFrom: workspace.productLineFrom ?? '',
    productLineTo: workspace.productLineTo ?? '',
    parentParts: workspace.parentParts && workspace.parentParts.length > 0 ? [...workspace.parentParts] : [],
    isTemporary: workspace.isTemporary,
    coverageEndsOn: workspace.coverageEndsOn ?? '',
  };
}

function hasMinimumScope(form: FormState): boolean {
  const site = form.site.trim();
  if (site.length !== 2 || !/^[A-Za-z]{2}$/.test(site)) return false;
  const hasParentPart = form.parentParts.some((p) => p.trim().length > 0);
  return hasParentPart || form.productLineFrom.trim().length > 0;
}

export function AddWorkspaceDialog({ workspace, onSave, onClose, onSavingChange }: AddWorkspaceDialogProps) {
  const isEditMode = workspace != null;
  const [form, setForm] = useState<FormState>(() => formFromWorkspace(workspace));
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<WorkspaceValidationErrors>({});
  const [parentPartsExpanded, setParentPartsExpanded] = useState(() => form.parentParts.length > 0);
  const dialogRef = useRef<HTMLDivElement>(null);
  const firstInputRef = useRef<HTMLInputElement>(null);

  const setSavingState = (value: boolean) => {
    setSaving(value);
    onSavingChange?.(value);
  };

  useEffect(() => {
    firstInputRef.current?.focus();
  }, []);

  // Trap focus inside dialog
  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLDivElement>) => {
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
  }, []);

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

  const setParentPart = (index: number) => (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setForm((prev) => {
      const next = [...prev.parentParts];
      next[index] = value;
      return { ...prev, parentParts: next };
    });
    if (fieldErrors['parentParts']) {
      setFieldErrors((prev) => {
        const next = { ...prev };
        delete next['parentParts'];
        return next;
      });
    }
  };

  const addParentPartRow = () => {
    setForm((prev) => ({ ...prev, parentParts: [...prev.parentParts, ''] }));
  };

  const removeParentPartRow = (index: number) => {
    setForm((prev) => ({ ...prev, parentParts: prev.parentParts.filter((_, i) => i !== index) }));
  };

  const toggleParentPartsExpanded = () => {
    setParentPartsExpanded((prev) => {
      const next = !prev;
      if (next && form.parentParts.length === 0) {
        setForm((prevForm) => ({ ...prevForm, parentParts: [''] }));
      }
      return next;
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!hasMinimumScope(form) || saving) return;

    setSavingState(true);
    setFieldErrors({});

    try {
      const trimmedParts = form.parentParts.map((p) => p.trim()).filter((p) => p.length > 0);
      await onSave({
        displayName: form.displayName.trim() || undefined,
        site: form.site.trim(),
        productLineFrom: form.productLineFrom.trim() || undefined,
        productLineTo: form.productLineTo.trim() || undefined,
        parentParts: trimmedParts,
        isTemporary: form.isTemporary,
        coverageEndsOn: form.isTemporary && form.coverageEndsOn ? form.coverageEndsOn : null,
      });
    } catch (err) {
      setSavingState(false);
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
            Enter a product-line range, one or more parent parts, or both. All entered filters will
            be applied together.
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

          <div className="dialog__field">
            <button
              type="button"
              className="dialog__collapsible-toggle"
              aria-expanded={parentPartsExpanded}
              aria-controls="dlg-parent-parts-section"
              onClick={toggleParentPartsExpanded}
            >
              {parentPartsExpanded ? '\u25be' : '\u25b8'} Limit to specific parent parts
            </button>
            {parentPartsExpanded && (
              <div id="dlg-parent-parts-section" className="dialog__parent-parts">
                {form.parentParts.map((part, index) => (
                  <div className="dialog__parent-part-row" key={index}>
                    <input
                      type="text"
                      className={`dialog__input${fieldError('parentParts') ? ' dialog__input--error' : ''}`}
                      value={part}
                      onChange={setParentPart(index)}
                      placeholder="Parent part number"
                      autoComplete="off"
                      aria-label={`Parent part ${index + 1}`}
                    />
                    <button
                      type="button"
                      className="dialog__btn dialog__btn--icon"
                      onClick={() => removeParentPartRow(index)}
                      aria-label={`Remove parent part ${index + 1}`}
                    >
                      −
                    </button>
                  </div>
                ))}
                <button
                  type="button"
                  className="dialog__btn dialog__btn--icon"
                  onClick={addParentPartRow}
                >
                  + Add parent part
                </button>
                {fieldError('parentParts') && (
                  <span className="dialog__field-error" role="alert">
                    {fieldError('parentParts')}
                  </span>
                )}
              </div>
            )}
          </div>

          {fieldErrors['scope'] && !fieldError('site') && (
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

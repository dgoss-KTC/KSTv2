import { useState } from 'react';
import type { SystemStatusResponse, UserPreferencesDto } from '../api/client';
import type { ConnectionState } from '../hooks/useBackendStatus';
import { useFiscalCalendarSettings } from '../hooks/useFiscalCalendarSettings';
import type { FiscalExceptionValidationError } from '../fiscal/fiscalCalendarSettings';
import './GeneralWorkspace.css';

interface GeneralWorkspaceProps {
  preferences: UserPreferencesDto;
  onSetTheme: (theme: UserPreferencesDto['theme']) => void;
  onSetAccentColor: (accentColor: UserPreferencesDto['accentColor']) => void;
  onSetRowDensity: (rowDensity: UserPreferencesDto['rowDensity']) => void;
  onOpenManageWorkspaces: () => void;
  connectionState: ConnectionState;
  status: SystemStatusResponse | null;
  onRefresh: () => void;
  isRefreshing: boolean;
}

const backendLabel: Record<ConnectionState, string> = {
  starting: 'Starting\u2026',
  waiting: 'Starting\u2026',
  connected: 'Connected',
  unavailable: 'Unavailable',
  api_error: 'Error',
};

function humanize(value: string): string {
  const spaced = value.replace(/([a-z])([A-Z])/g, '$1 $2');
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}

function formatTimestamp(value: string | null | undefined): string {
  if (!value) return 'Never';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Never';
  return date.toLocaleString();
}

function formatAnchorDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' });
}

export function GeneralWorkspace({
  preferences,
  onSetTheme,
  onSetAccentColor,
  onSetRowDensity,
  onOpenManageWorkspaces,
  connectionState,
  status,
  onRefresh,
  isRefreshing,
}: GeneralWorkspaceProps) {
  const qad = status?.dataSources.find((d) => d.name === 'QAD');
  const shortages = status?.dataSources.find((d) => d.name === 'Shortage Database');

  const { settings: fiscalSettings, addException, removeException } = useFiscalCalendarSettings();
  const [isAddingException, setIsAddingException] = useState(false);
  const [exceptionFiscalYear, setExceptionFiscalYear] = useState('');
  const [exceptionPeriod, setExceptionPeriod] = useState('');
  const [exceptionErrors, setExceptionErrors] = useState<FiscalExceptionValidationError[]>([]);

  function errorFor(field: FiscalExceptionValidationError['field']): string | undefined {
    return exceptionErrors.find((e) => e.field === field)?.message;
  }

  function resetExceptionForm() {
    setIsAddingException(false);
    setExceptionFiscalYear('');
    setExceptionPeriod('');
    setExceptionErrors([]);
  }

  function handleSaveException() {
    const errors = addException({
      fiscalYear: Number(exceptionFiscalYear),
      extraWeekPeriod: Number(exceptionPeriod),
    });
    if (errors.length > 0) {
      setExceptionErrors(errors);
      return;
    }
    resetExceptionForm();
  }

  return (
    <div className="general-workspace">
      <h2 className="general-workspace__title">General</h2>

      <section className="general-workspace__section">
        <h3 className="general-workspace__section-title">Appearance</h3>

        <div className="general-workspace__field">
          <span className="general-workspace__field-label">Theme</span>
          <div className="segmented" role="group" aria-label="Theme">
            {(['system', 'light', 'dark'] as const).map((option) => (
              <button
                key={option}
                type="button"
                className={`segmented__btn${preferences.theme === option ? ' segmented__btn--active' : ''}`}
                aria-pressed={preferences.theme === option}
                onClick={() => onSetTheme(option)}
              >
                {humanize(option)}
              </button>
            ))}
          </div>
        </div>

        <div className="general-workspace__field">
          <span className="general-workspace__field-label">Accent color</span>
          <div className="segmented" role="group" aria-label="Accent color">
            {(['blue', 'teal', 'amber'] as const).map((option) => (
              <button
                key={option}
                type="button"
                className={`segmented__btn${preferences.accentColor === option ? ' segmented__btn--active' : ''}`}
                aria-pressed={preferences.accentColor === option}
                onClick={() => onSetAccentColor(option)}
              >
                {humanize(option)}
              </button>
            ))}
          </div>
        </div>

        <div className="general-workspace__field">
          <span className="general-workspace__field-label">Row density</span>
          <div className="segmented" role="group" aria-label="Row density">
            {(['compact', 'comfortable'] as const).map((option) => (
              <button
                key={option}
                type="button"
                className={`segmented__btn${preferences.rowDensity === option ? ' segmented__btn--active' : ''}`}
                aria-pressed={preferences.rowDensity === option}
                onClick={() => onSetRowDensity(option)}
              >
                {humanize(option)}
              </button>
            ))}
          </div>
        </div>
      </section>

      <section className="general-workspace__section">
        <h3 className="general-workspace__section-title">Workspace Management</h3>
        <p className="general-workspace__hint">
          Add, edit, archive, restore, or delete workspace assignments.
        </p>
        <button
          type="button"
          className="general-workspace__btn"
          onClick={onOpenManageWorkspaces}
        >
          Manage Workspaces&hellip;
        </button>
      </section>

      <section className="general-workspace__section">
        <h3 className="general-workspace__section-title">Fiscal Calendar</h3>

        <dl className="general-workspace__details">
          <dt>Anchor fiscal year</dt>
          <dd>{`FY${fiscalSettings.anchorFiscalYear}`}</dd>

          <dt>Anchor start date</dt>
          <dd>{formatAnchorDate(fiscalSettings.anchorStartDate)}</dd>

          <dt>Pattern</dt>
          <dd>4-4-5 &times; 4</dd>
        </dl>

        {fiscalSettings.exceptions.length > 0 && (
          <table className="general-workspace__table">
            <thead>
              <tr>
                <th>Fiscal Year</th>
                <th>Extra Week Period</th>
                <th aria-label="Actions" />
              </tr>
            </thead>
            <tbody>
              {fiscalSettings.exceptions.map((exception) => (
                <tr key={exception.fiscalYear}>
                  <td>{`FY${exception.fiscalYear}`}</td>
                  <td>{`P${exception.extraWeekPeriod}`}</td>
                  <td>
                    <button
                      type="button"
                      className="general-workspace__btn"
                      onClick={() => removeException(exception.fiscalYear)}
                      aria-label={`Remove FY${exception.fiscalYear} exception`}
                    >
                      Remove
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        {isAddingException ? (
          <div className="general-workspace__exception-form">
            <label className="general-workspace__field">
              <span className="general-workspace__field-label">Fiscal year</span>
              <input
                type="number"
                value={exceptionFiscalYear}
                onChange={(e) => setExceptionFiscalYear(e.target.value)}
                aria-label="Fiscal year"
              />
            </label>
            {errorFor('fiscalYear') && (
              <p className="general-workspace__error">{errorFor('fiscalYear')}</p>
            )}

            <label className="general-workspace__field">
              <span className="general-workspace__field-label">Extra week period (1-12)</span>
              <input
                type="number"
                value={exceptionPeriod}
                onChange={(e) => setExceptionPeriod(e.target.value)}
                aria-label="Extra week period"
              />
            </label>
            {errorFor('extraWeekPeriod') && (
              <p className="general-workspace__error">{errorFor('extraWeekPeriod')}</p>
            )}

            <div className="general-workspace__exception-form-actions">
              <button type="button" className="general-workspace__btn" onClick={handleSaveException}>
                Save
              </button>
              <button type="button" className="general-workspace__btn" onClick={resetExceptionForm}>
                Cancel
              </button>
            </div>
          </div>
        ) : (
          <button
            type="button"
            className="general-workspace__btn"
            onClick={() => setIsAddingException(true)}
          >
            + Add Exception
          </button>
        )}
      </section>

      <section className="general-workspace__section">
        <h3 className="general-workspace__section-title">Application Status</h3>

        <dl className="general-workspace__details">
          <dt>Backend</dt>
          <dd>{backendLabel[connectionState]}</dd>

          <dt>Snapshot</dt>
          <dd>{status ? humanize(status.snapshot.status) : 'Unknown'}</dd>

          <dt>QAD</dt>
          <dd>{qad ? humanize(qad.status) : 'Unknown'}</dd>

          <dt>Shortage Database</dt>
          <dd>{shortages ? humanize(shortages.status) : 'Unknown'}</dd>

          <dt>Last successful refresh</dt>
          <dd>{formatTimestamp(status?.lastSuccessfulRefreshAt)}</dd>
        </dl>

        <button
          type="button"
          className="general-workspace__btn"
          onClick={onRefresh}
          disabled={isRefreshing}
        >
          {isRefreshing ? 'Refreshing\u2026' : 'Refresh'}
        </button>
      </section>
    </div>
  );
}

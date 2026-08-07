import type { WorkspaceAssignmentDto } from '../api/client';
import { useFiscalCalendarSettings } from '../hooks/useFiscalCalendarSettings';
import {
  MAX_MPS_HORIZON_WEEKS,
  MIN_MPS_HORIZON_WEEKS,
  useMpsDashboard,
} from '../hooks/useMpsDashboard';
import { getFiscalDisplayInfo } from '../fiscal/fiscalCalendar';
import {
  describeBucket,
  executionStatusClass,
  formatQuantity,
  formatWeekLabel,
  groupConsecutive,
  parseIsoDateOnly,
} from '../mps/mpsPresentation';
import './MpsWorkspace.css';

interface MpsWorkspaceProps {
  workspace: WorkspaceAssignmentDto;
}

const SNAPSHOT_STATUS_LABELS: Record<string, string> = {
  notLoaded: 'Not loaded',
  loading: 'Loading',
  current: 'Current',
  stale: 'Stale',
  partial: 'Partial',
  failed: 'Failed',
};

function formatTimestamp(value: string | null | undefined): string {
  if (!value) return 'Never';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Never';
  return date.toLocaleString();
}

export function MpsWorkspace({ workspace }: MpsWorkspaceProps) {
  const {
    dashboard,
    dateBasis,
    horizonWeeks,
    isLoading,
    isRefreshing,
    error,
    setDateBasis,
    setHorizonWeeks,
    reload,
    refresh,
  } = useMpsDashboard(workspace.assignmentId);
  const { settings: fiscalSettings } = useFiscalCalendarSettings();

  const title = workspace.displayName ?? workspace.site;
  const parts = dashboard?.parts ?? [];
  const weeklyBuckets = parts[0]?.buckets.slice(1) ?? [];

  const weekFiscalInfo = weeklyBuckets.map((bucket) =>
    bucket.weekLabel
      ? getFiscalDisplayInfo(fiscalSettings, parseIsoDateOnly(bucket.weekLabel))
      : null,
  );
  const quarterGroups = groupConsecutive(weekFiscalInfo, (info) =>
    info ? `${info.fiscalYear}-Q${info.fiscalQuarter}` : 'n/a',
  );
  const periodGroups = groupConsecutive(weekFiscalInfo, (info) =>
    info ? `${info.fiscalYear}-P${info.fiscalPeriod}` : 'n/a',
  );

  return (
    <div className="mps-workspace">
      <header className="mps-workspace__header">
        <div className="mps-workspace__title-group">
          <h2 className="mps-workspace__title">{title}</h2>
          {dashboard && (
            <span className="mps-workspace__meta">
              {SNAPSHOT_STATUS_LABELS[dashboard.snapshot.status] ?? dashboard.snapshot.status}
              {' \u00b7 Last refresh: '}
              {formatTimestamp(dashboard.snapshot.lastSuccessfulRefreshAtUtc)}
              {' \u00b7 '}
              {dashboard.snapshot.resolvedParentPartCount} parts
            </span>
          )}
        </div>

        <div className="mps-workspace__controls">
          <div className="segmented" role="group" aria-label="Date basis">
            {(['dueDate', 'releaseDate'] as const).map((option) => (
              <button
                key={option}
                type="button"
                className={`segmented__btn${dateBasis === option ? ' segmented__btn--active' : ''}`}
                aria-pressed={dateBasis === option}
                onClick={() => setDateBasis(option)}
              >
                {option === 'dueDate' ? 'Due Date' : 'Release Date'}
              </button>
            ))}
          </div>

          <label className="mps-workspace__horizon">
            <span>Horizon (weeks)</span>
            <input
              type="number"
              min={MIN_MPS_HORIZON_WEEKS}
              max={MAX_MPS_HORIZON_WEEKS}
              value={horizonWeeks}
              onChange={(e) => {
                const next = Number(e.target.value);
                if (Number.isInteger(next) && next >= MIN_MPS_HORIZON_WEEKS && next <= MAX_MPS_HORIZON_WEEKS) {
                  setHorizonWeeks(next);
                }
              }}
              aria-label="Horizon in weeks"
            />
          </label>

          <button
            type="button"
            className="mps-workspace__btn"
            onClick={() => void refresh()}
            disabled={isRefreshing}
          >
            {isRefreshing ? 'Refreshing\u2026' : 'Refresh'}
          </button>
        </div>
      </header>

      {error && dashboard && (
        <div className="mps-workspace__banner mps-workspace__banner--warning" role="alert">
          {error.type === 'unavailable' ? error.detail : 'The last action could not be completed.'}
          {' Showing last known data.'}
        </div>
      )}

      {isLoading && !dashboard && (
        <div className="mps-workspace__state">Loading MPS data&hellip;</div>
      )}

      {!isLoading && error && !dashboard && (
        <div className="mps-workspace__state mps-workspace__state--error">
          <p>{error.type === 'unavailable' ? error.detail : 'Could not load MPS data.'}</p>
          <button type="button" className="mps-workspace__btn" onClick={() => void reload()}>
            Retry
          </button>
        </div>
      )}

      {!isLoading && dashboard && parts.length === 0 && (
        <div className="mps-workspace__state">
          No parts resolved for this workspace&apos;s current scope.
        </div>
      )}

      {!isLoading && dashboard && parts.length > 0 && (
        <div className="mps-grid-scroll">
          <table className="mps-grid">
            <thead>
              <tr>
                <th className="mps-grid__sticky mps-grid__part-col" rowSpan={3}>
                  Parent Part
                </th>
                <th className="mps-grid__sticky mps-grid__falldown-col" rowSpan={3}>
                  Falldown
                </th>
                {quarterGroups.map((group, idx) => (
                  <th key={`q-${idx}`} colSpan={group.span}>
                    {group.key === 'n/a' ? '' : `Q${group.key.split('-Q')[1]}`}
                  </th>
                ))}
              </tr>
              <tr>
                {periodGroups.map((group, idx) => (
                  <th key={`p-${idx}`} colSpan={group.span}>
                    {group.key === 'n/a' ? '' : `P${group.key.split('-P')[1]}`}
                  </th>
                ))}
              </tr>
              <tr>
                {weeklyBuckets.map((bucket, idx) => (
                  <th key={bucket.weekLabel ?? `week-${idx}`}>{formatWeekLabel(bucket.weekLabel)}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {parts.map((part) => {
                const falldown = part.buckets[0];
                const weekly = part.buckets.slice(1);
                return (
                  <tr key={part.parentPart}>
                    <td className="mps-grid__sticky mps-grid__part-col">
                      <div className="mps-grid__part-number">{part.parentPart}</div>
                      <div className="mps-grid__part-desc">{part.description ?? '\u2014'}</div>
                    </td>
                    <td
                      className="mps-grid__sticky mps-grid__falldown-col mps-cell"
                      title={falldown ? describeBucket(falldown) : undefined}
                    >
                      {falldown ? formatQuantity(falldown.quantity) : '\u2014'}
                      <span className="visually-hidden">{falldown ? describeBucket(falldown) : ''}</span>
                    </td>
                    {weekly.map((bucket, idx) => (
                      <td
                        key={bucket.weekLabel ?? `week-${idx}`}
                        className={[
                          'mps-cell',
                          `mps-cell--${executionStatusClass(bucket.executionStatus)}`,
                          bucket.containsPlannedWork ? 'mps-cell--planned' : '',
                          bucket.containsExplicitlyScheduledWork ? 'mps-cell--explicit' : '',
                        ]
                          .filter(Boolean)
                          .join(' ')}
                        title={describeBucket(bucket)}
                      >
                        {formatQuantity(bucket.quantity)}
                        <span className="visually-hidden">{describeBucket(bucket)}</span>
                      </td>
                    ))}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

import { useEffect, useRef, useState } from 'react';
import type { WorkspaceAssignmentDto } from '../api/client';
import { useFiscalCalendarSettings } from '../hooks/useFiscalCalendarSettings';
import {
  MAX_MPS_HORIZON_WEEKS,
  MIN_MPS_HORIZON_WEEKS,
  useMpsDashboard,
} from '../hooks/useMpsDashboard';
import { usePartDetail } from '../hooks/usePartDetail';
import { useBucketWorkOrders } from '../hooks/useBucketWorkOrders';
import { PartInfoPanel } from './PartInfoPanel';
import { WorkOrdersPanel } from './WorkOrdersPanel';
import { getFiscalDisplayInfo } from '../fiscal/fiscalCalendar';
import {
  type BucketSelection,
  bucketCellClassNames,
  describeBucket,
  describeBucketSelection,
  formatQuantity,
  formatWeekLabel,
  groupConsecutive,
  isWeeklyBucketWorkOrderEligible,
  parseIsoDateOnly,
  withPeriodColor,
  withQuarterColor,
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

  // Parent/bucket selection and the active detail tab are transient UI state, not persisted
  // workspace configuration; they reset whenever the workspace context changes (see the accepted
  // Stage 6 contract §2, extended by the accepted Stage 7 contract §4).
  const [selectedParent, setSelectedParent] = useState<string | null>(null);
  const [selectedBucket, setSelectedBucket] = useState<BucketSelection | null>(null);
  const [activeTab, setActiveTab] = useState<'partInfo' | 'workOrders'>('partInfo');
  useEffect(() => {
    const id = setTimeout(() => {
      setSelectedParent(null);
      setSelectedBucket(null);
      setActiveTab('partInfo');
    }, 0);
    return () => clearTimeout(id);
  }, [workspace.assignmentId]);

  // A successful workspace refresh atomically replaces the MPS snapshot; prior Stage 7 drill-down
  // context (selected bucket, Work Orders tab, and every nested WO/material/candidate expansion it
  // holds) becomes invalid and must be cleared so nothing stale appears beneath the new snapshot
  // generation (accepted Stage 7 contract §19). A failed refresh leaves the retained last-good
  // snapshot id unchanged, so this effect intentionally does not fire and existing drill-down state
  // is preserved.
  const previousSnapshotIdRef = useRef<string | null>(null);
  useEffect(() => {
    const currentSnapshotId = dashboard?.snapshot.snapshotId ?? null;
    const previousSnapshotId = previousSnapshotIdRef.current;
    previousSnapshotIdRef.current = currentSnapshotId;

    if (previousSnapshotId !== null && currentSnapshotId !== null && previousSnapshotId !== currentSnapshotId) {
      setSelectedBucket(null);
      setActiveTab('partInfo');
    }
  }, [dashboard?.snapshot.snapshotId]);

  const { detail: partDetail, isLoading: isPartDetailLoading, error: partDetailError, retry: retryPartDetail } =
    usePartDetail(workspace.assignmentId, selectedParent);

  const {
    workOrders: bucketWorkOrders,
    isLoading: isBucketWorkOrdersLoading,
    error: bucketWorkOrdersError,
    retry: retryBucketWorkOrders,
  } = useBucketWorkOrders(
    workspace.assignmentId,
    dashboard?.snapshot.snapshotId ?? null,
    selectedBucket,
    dateBasis,
    horizonWeeks,
  );

  const clearSelection = () => {
    setSelectedParent(null);
    setSelectedBucket(null);
    setActiveTab('partInfo');
  };

  // Parent-row selection is a toggle: selecting the already-selected parent closes the detail
  // panel entirely and returns to the full grid, using the same clear-selection path as the
  // explicit Back button. Selecting a different (or first) parent focuses it, clears any bucket
  // context, and opens/retains Part Info — Work Orders (and later contextual tabs) stay disabled
  // until a schedule bucket is selected.
  function handleParentRowSelect(partNumber: string) {
    if (selectedParent === partNumber) {
      clearSelection();
      return;
    }
    setSelectedParent(partNumber);
    setSelectedBucket(null);
    setActiveTab('partInfo');
  }

  // Bucket selection (Falldown or an eligible weekly cell) selects the parent + bucket together
  // and automatically opens Work Orders, regardless of what was previously selected.
  function handleBucketSelect(
    partNumber: string,
    kind: BucketSelection['kind'],
    weekLabel: string | null,
    e: React.MouseEvent,
  ) {
    e.stopPropagation();
    setSelectedParent(partNumber);
    setSelectedBucket({ parentPart: partNumber, kind, weekLabel });
    setActiveTab('workOrders');
  }

  const title = workspace.displayName ?? workspace.site;
  const allParts = dashboard?.parts ?? [];
  const parts = selectedParent ? allParts.filter((p) => p.parentPart === selectedParent) : allParts;
  const weeklyBuckets = allParts[0]?.buckets.slice(1) ?? [];

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

  // Fiscal-quarter color family (1-4, cycling blue/green/amber/purple regardless of fiscal year —
  // see MpsWorkspace.css `.mps-grid__band-col--q1..4`). Period bands inherit their parent quarter's
  // family and alternate a reduced tint per period within that quarter, mirroring the prototype's
  // `qColors` / `periodBandsV` construction.
  const quarterBands = withQuarterColor(quarterGroups, weekFiscalInfo);
  const periodBands = withPeriodColor(periodGroups, weekFiscalInfo);

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
            className="mps-workspace__btn mps-workspace__btn--primary"
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
          <button type="button" className="mps-workspace__btn mps-workspace__btn--primary" onClick={() => void reload()}>
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
        <div className={`mps-grid-frame${selectedParent ? ' mps-grid-frame--focused' : ''}`}>
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
                  {quarterBands.map((group, idx) => (
                    <th
                      key={`q-${idx}`}
                      className={`mps-grid__band-col mps-grid__band-col--q${group.quarterNum}`}
                      colSpan={group.span}
                    >
                      {group.key === 'n/a' ? '' : `Q${group.key.split('-Q')[1]}`}
                    </th>
                  ))}
                </tr>
                <tr>
                  {periodBands.map((group, idx) => (
                    <th
                      key={`p-${idx}`}
                      className={`mps-grid__band-col mps-grid__band-col--q${group.quarterNum}${
                        group.altShade ? ' mps-grid__band-col--alt' : ''
                      }`}
                      colSpan={group.span}
                    >
                      {group.key === 'n/a' ? '' : `P${group.key.split('-P')[1]}`}
                    </th>
                  ))}
                </tr>
                <tr>
                  {weeklyBuckets.map((bucket, idx) => (
                    <th key={bucket.weekLabel ?? `week-${idx}`} className="mps-grid__week-col">
                      {formatWeekLabel(bucket.weekLabel)}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {parts.map((part) => {
                  const falldown = part.buckets[0];
                  const weekly = part.buckets.slice(1);
                  const isSelected = part.parentPart === selectedParent;
                  const isFalldownSelected =
                    selectedBucket?.parentPart === part.parentPart && selectedBucket.kind === 'falldown';
                  return (
                    <tr
                      key={part.parentPart}
                      className={isSelected ? 'mps-grid__row--selected' : undefined}
                      onClick={() => handleParentRowSelect(part.parentPart)}
                      aria-selected={isSelected}
                      tabIndex={0}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault();
                          handleParentRowSelect(part.parentPart);
                        }
                      }}
                    >
                      <td className="mps-grid__sticky mps-grid__part-col">
                        <div className="mps-grid__part-cell">
                          <span className="mps-grid__part-number">{part.parentPart}</span>
                          <span className="mps-grid__part-desc" title={part.description ?? undefined}>
                            {part.description ?? '\u2014'}
                          </span>
                        </div>
                      </td>
                      <td
                        className={`mps-grid__sticky mps-grid__falldown-col${
                          falldown ? ` ${bucketCellClassNames(falldown)}` : ' mps-cell'
                        }${falldown ? ' mps-cell--clickable' : ''}${
                          isFalldownSelected ? ' mps-cell--selected-bucket' : ''
                        }`}
                        title={falldown ? describeBucket(falldown) : undefined}
                        onClick={
                          falldown
                            ? (e) => handleBucketSelect(part.parentPart, 'falldown', null, e)
                            : undefined
                        }
                      >
                        {falldown ? formatQuantity(falldown.quantity) : '\u2014'}
                        <span className="visually-hidden">{falldown ? describeBucket(falldown) : ''}</span>
                      </td>
                      {weekly.map((bucket, idx) => {
                        const isEligible = isWeeklyBucketWorkOrderEligible(idx);
                        const isBucketSelected =
                          selectedBucket?.parentPart === part.parentPart &&
                          selectedBucket.kind === 'weekly' &&
                          selectedBucket.weekLabel === bucket.weekLabel;
                        return (
                          <td
                            key={bucket.weekLabel ?? `week-${idx}`}
                            className={`mps-grid__week-col ${bucketCellClassNames(bucket)}${
                              isEligible ? ' mps-cell--clickable' : ''
                            }${isBucketSelected ? ' mps-cell--selected-bucket' : ''}`}
                            title={describeBucket(bucket)}
                            onClick={
                              isEligible
                                ? (e) => handleBucketSelect(part.parentPart, 'weekly', bucket.weekLabel, e)
                                : undefined
                            }
                          >
                            {formatQuantity(bucket.quantity)}
                            <span className="visually-hidden">{describeBucket(bucket)}</span>
                          </td>
                        );
                      })}
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {selectedParent && (
        <div className="mps-detail">
          <div className="mps-detail__tabs" role="tablist" aria-label="Part detail">
            <button
              type="button"
              role="tab"
              aria-selected={activeTab === 'partInfo'}
              className={`mps-detail__tab${activeTab === 'partInfo' ? ' mps-detail__tab--active' : ''}`}
              onClick={() => setActiveTab('partInfo')}
            >
              Part Info
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={activeTab === 'workOrders'}
              disabled={!selectedBucket}
              className={`mps-detail__tab${activeTab === 'workOrders' ? ' mps-detail__tab--active' : ''}`}
              onClick={() => selectedBucket && setActiveTab('workOrders')}
            >
              Work Orders
            </button>
            <button type="button" role="tab" aria-selected={false} className="mps-detail__tab" disabled>
              Shortages
            </button>
            <button type="button" role="tab" aria-selected={false} className="mps-detail__tab" disabled>
              Future Shortages
            </button>
            <button type="button" role="tab" aria-selected={false} className="mps-detail__tab" disabled>
              Components
            </button>
          </div>

          {activeTab === 'partInfo' && (
            <PartInfoPanel
              partNumber={selectedParent}
              detail={partDetail}
              isLoading={isPartDetailLoading}
              error={partDetailError}
              onRetry={() => void retryPartDetail()}
              onBack={clearSelection}
            />
          )}

          {activeTab === 'workOrders' && selectedBucket && (
            <WorkOrdersPanel
              parentPart={selectedBucket.parentPart}
              bucketLabel={describeBucketSelection(selectedBucket)}
              assignmentId={workspace.assignmentId}
              snapshotId={dashboard?.snapshot.snapshotId ?? null}
              workOrders={bucketWorkOrders}
              isLoading={isBucketWorkOrdersLoading}
              error={bucketWorkOrdersError}
              onRetry={() => void retryBucketWorkOrders()}
            />
          )}
        </div>
      )}
    </div>
  );
}

import { useEffect, useMemo, useRef, useState } from 'react';
import type { WorkspaceAssignmentDto } from '../api/client';
import { useFiscalCalendarSettings } from '../hooks/useFiscalCalendarSettings';
import {
  MAX_MPS_HORIZON_WEEKS,
  MIN_MPS_HORIZON_WEEKS,
  useMpsDashboard,
} from '../hooks/useMpsDashboard';
import { usePartDetail } from '../hooks/usePartDetail';
import { usePlanningWindowWorkOrders } from '../hooks/usePlanningWindowWorkOrders';
import { useBom } from '../hooks/useBom';
import { useComponentDetail } from '../hooks/useComponentDetail';
import { useApprovedVendors } from '../hooks/useApprovedVendors';
import { PartInfoPanel } from './PartInfoPanel';
import { BomPanel } from './BomPanel';
import { ComponentInfoModal } from './ComponentInfoModal';
import { WorkOrdersPanel } from './WorkOrdersPanel';
import { getFiscalDisplayInfo } from '../fiscal/fiscalCalendar';
import { EscapeStackContext, type EscapeStackEntry } from '../mps/escapeStack';
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
  const [activeTab, setActiveTab] = useState<'partInfo' | 'bom' | 'workOrders'>('partInfo');
  // Stage 8D.6: the component currently inspected in the blocking Component Information modal,
  // plus the originating BOM row so focus can be restored to it on close. Modal state is owned
  // here (not by BomPanel) so it survives independently of BOM filter/search state.
  const [inspectedComponent, setInspectedComponent] = useState<{
    componentPart: string;
    returnFocusEl: HTMLElement | null;
  } | null>(null);
  // Keyed by parentPart so Escape can restore focus to the exact grid row that was drilled into,
  // even though the row stays mounted (never removed) while its detail view is open.
  const rowRefs = useRef<Map<string, HTMLTableRowElement>>(new Map());
  // LIFO stack of nested Work Orders drill-down expansions (material lines, candidate branches)
  // registered by descendants via `useEscapeLevel`; the api object is memoized once (stable
  // identity) so descendants' effects don't re-run on every MpsWorkspace render.
  const escapeStackRef = useRef<EscapeStackEntry[]>([]);
  const escapeStackApi = useMemo(
    () => ({
      push: (entry: EscapeStackEntry) => escapeStackRef.current.push(entry),
      remove: (id: string) => {
        escapeStackRef.current = escapeStackRef.current.filter((e) => e.id !== id);
      },
      popTop: () => {
        const top = escapeStackRef.current.pop();
        if (!top) return false;
        top.collapse();
        return true;
      },
    }),
    [],
  );
  useEffect(() => {
    const id = setTimeout(() => {
      setSelectedParent(null);
      setSelectedBucket(null);
      setActiveTab('partInfo');
      setInspectedComponent(null);
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
      setInspectedComponent(null);
    }
  }, [dashboard?.snapshot.snapshotId]);

  const { detail: partDetail, isLoading: isPartDetailLoading, error: partDetailError, retry: retryPartDetail } =
    usePartDetail(workspace.assignmentId, selectedParent);

  // Stage 7R: the Work Orders population is the parent-scoped four-week planning window, sourced
  // from the same capability for both the parent-level view (selectedBucket null) and the
  // bucket-filtered view (selectedBucket set).
  const {
    workOrders: planningWindowWorkOrders,
    isLoading: isPlanningWindowLoading,
    error: planningWindowError,
    retry: retryPlanningWindow,
  } = usePlanningWindowWorkOrders(
    workspace.assignmentId,
    dashboard?.snapshot.snapshotId ?? null,
    selectedParent,
    dateBasis,
    selectedBucket,
  );

  // BOM is parent-contextual (never bucket/basis/horizon-contextual) and lazy: the request fires
  // only on the first explicit BOM-tab activation for the current (workspace, parent, snapshot)
  // identity — the tab click is the only path that starts a request, so a successful MPS refresh
  // (new snapshot id) invalidates the loaded BOM with no transient request and the next explicit
  // activation re-requests; a failed refresh leaves the snapshot id — and the displayed BOM —
  // untouched. Obsolete in-flight responses never commit to state (useBom).
  const { bom, isLoading: isBomLoading, error: bomError, activate: activateBom, retry: retryBom } = useBom(
    workspace.assignmentId,
    selectedParent,
    dashboard?.snapshot.snapshotId ?? null,
  );

  // Component Detail is lazy per Stage 8D.6: it loads only while a component is being inspected
  // (a BOM row was clicked) and never preloads for the whole BOM. Closing the modal
  // (`inspectedComponent = null`) immediately invalidates the identity so a late response for a
  // just-closed or just-replaced component can never populate the modal (see useComponentDetail).
  const {
    detail: componentDetail,
    isLoading: isComponentDetailLoading,
    error: componentDetailError,
    retry: retryComponentDetail,
  } = useComponentDetail(workspace.assignmentId, inspectedComponent?.componentPart ?? null);

  // Approved Vendors (Stage 8D.7) is independently lazy: no request until the modal's AVL section
  // is explicitly expanded. Owned at this level (mirroring Component Detail) so the modal itself
  // stays a pure render/props component.
  const {
    rows: approvedVendors,
    isLoading: isApprovedVendorsLoading,
    error: approvedVendorsError,
    activate: activateApprovedVendors,
    retry: retryApprovedVendors,
  } = useApprovedVendors(workspace.assignmentId, inspectedComponent?.componentPart ?? null);

  function handleSelectComponent(componentPart: string, rowElement: HTMLElement) {
    setInspectedComponent({ componentPart, returnFocusEl: rowElement });
  }

  function handleCloseComponentInfo() {
    const returnFocusEl = inspectedComponent?.returnFocusEl ?? null;
    setInspectedComponent(null);
    if (returnFocusEl && returnFocusEl.isConnected) {
      returnFocusEl.focus();
    }
  }

  function clearSelection() {
    const partToRefocus = selectedParent;
    setSelectedParent(null);
    setSelectedBucket(null);
    setActiveTab('partInfo');
    setInspectedComponent(null);
    // Deferred: the detail panel unmounting and the grid reverting to its full (unfiltered) row
    // set both commit in this same update: check connectivity only after that commit lands.
    if (partToRefocus) {
      setTimeout(() => {
        const row = rowRefs.current.get(partToRefocus);
        if (row?.isConnected) row.focus();
      }, 0);
    }
  }

  // Single document-level, capture-phase Escape that pops one level of the MPS drill-down per
  // press: first any registered nested Work Orders expansion (material lines, candidate branch —
  // most-recently-opened first), then the whole detail panel (Part Info/BOM/Work Orders are all
  // one "detail" level below the grid) back to the full grid — the same action as clicking the
  // already-selected parent row again. Component Information (a BOM row drill-down) owns its own
  // independent Escape handling and stays open here; this handler is a no-op while it is open so a
  // single Escape press cannot close both at once.
  useEffect(() => {
    function handleDocumentEscape(e: KeyboardEvent) {
      if (e.key !== 'Escape') return;
      if (inspectedComponent) return;
      if (escapeStackApi.popTop()) {
        e.preventDefault();
        e.stopPropagation();
        return;
      }
      if (!selectedParent) return;
      e.preventDefault();
      e.stopPropagation();
      clearSelection();
    }
    document.addEventListener('keydown', handleDocumentEscape, true);
    return () => document.removeEventListener('keydown', handleDocumentEscape, true);
  });

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
  // and automatically opens Work Orders, regardless of what was previously selected. Accepts either
  // a mouse or keyboard event (Enter/Space) so activation is identical for both input methods.
  function handleBucketSelect(
    partNumber: string,
    kind: BucketSelection['kind'],
    weekLabel: string | null,
    e: React.SyntheticEvent,
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
                      ref={(el) => {
                        if (el) rowRefs.current.set(part.parentPart, el);
                        else rowRefs.current.delete(part.parentPart);
                      }}
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
                        tabIndex={falldown ? 0 : undefined}
                        onClick={
                          falldown
                            ? (e) => handleBucketSelect(part.parentPart, 'falldown', null, e)
                            : undefined
                        }
                        onKeyDown={
                          falldown
                            ? (e) => {
                                if (e.key === 'Enter' || e.key === ' ') {
                                  e.preventDefault();
                                  handleBucketSelect(part.parentPart, 'falldown', null, e);
                                }
                              }
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
                            tabIndex={isEligible ? 0 : undefined}
                            onClick={
                              isEligible
                                ? (e) => handleBucketSelect(part.parentPart, 'weekly', bucket.weekLabel, e)
                                : undefined
                            }
                            onKeyDown={
                              isEligible
                                ? (e) => {
                                    if (e.key === 'Enter' || e.key === ' ') {
                                      e.preventDefault();
                                      handleBucketSelect(part.parentPart, 'weekly', bucket.weekLabel, e);
                                    }
                                  }
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
              aria-selected={activeTab === 'bom'}
              className={`mps-detail__tab${activeTab === 'bom' ? ' mps-detail__tab--active' : ''}`}
              onClick={() => {
                setActiveTab('bom');
                activateBom();
              }}
            >
              BOM
            </button>
            {/* Stage 7R: Work Orders is available whenever a parent is selected (parent-level
                planning window, or bucket-filtered when a bucket is also selected). Shortages
                remains deferred/disabled. */}
            <button
              type="button"
              role="tab"
              aria-selected={activeTab === 'workOrders'}
              className={`mps-detail__tab${activeTab === 'workOrders' ? ' mps-detail__tab--active' : ''}`}
              onClick={() => setActiveTab('workOrders')}
            >
              Work Orders
            </button>
            <button type="button" role="tab" aria-selected={false} className="mps-detail__tab" disabled>
              Shortages
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

          {activeTab === 'bom' && (
            <BomPanel
              parentPart={selectedParent}
              bom={bom}
              isLoading={isBomLoading}
              error={bomError}
              onRetry={() => void retryBom()}
              onSelectComponent={handleSelectComponent}
            />
          )}

          {activeTab === 'workOrders' && (
            <EscapeStackContext.Provider value={escapeStackApi}>
              <WorkOrdersPanel
                parentPart={selectedParent}
                bucketLabel={selectedBucket ? describeBucketSelection(selectedBucket) : 'Planning window'}
                assignmentId={workspace.assignmentId}
                snapshotId={dashboard?.snapshot.snapshotId ?? null}
                workOrders={planningWindowWorkOrders}
                isLoading={isPlanningWindowLoading}
                error={planningWindowError}
                onRetry={() => void retryPlanningWindow()}
                dateBasis={dateBasis}
              />
            </EscapeStackContext.Provider>
          )}
        </div>
      )}

      {inspectedComponent && (
        <ComponentInfoModal
          componentPart={inspectedComponent.componentPart}
          detail={componentDetail}
          isLoading={isComponentDetailLoading}
          error={componentDetailError}
          onRetry={() => void retryComponentDetail()}
          onClose={handleCloseComponentInfo}
          approvedVendors={approvedVendors}
          isApprovedVendorsLoading={isApprovedVendorsLoading}
          approvedVendorsError={approvedVendorsError}
          onExpandApprovedVendors={activateApprovedVendors}
          onRetryApprovedVendors={() => void retryApprovedVendors()}
        />
      )}
    </div>
  );
}

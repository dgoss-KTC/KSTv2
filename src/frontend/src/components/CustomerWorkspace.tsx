import { useCallback, useEffect, useState } from 'react';
import type { WorkspaceAssignmentDto } from '../api/client';
import { useMpsDashboard } from '../hooks/useMpsDashboard';
import { usePlanningProposals } from '../hooks/usePlanningProposals';
import { ComponentOrdersPanel } from './ComponentOrdersPanel';
import { LongTermShortagesPanel } from './LongTermShortagesPanel';
import { MpsWorkspace } from './MpsWorkspace';
import { OpenOrdersPanel } from './OpenOrdersPanel';
import { PlanningWorkbookPanel } from './PlanningWorkbookPanel';
import { FinishedGoodsPanel } from './FinishedGoodsPanel';
import { WorkspaceShipmentsPanel } from './WorkspaceShipmentsPanel';
import './CustomerWorkspace.css';

type CustomerModule = 'dashboard' | 'planning' | 'openOrders' | 'componentOrders' | 'longTermShortages' | 'finishedGoods' | 'workspaceShipments';

const modules: ReadonlyArray<{ id: CustomerModule; label: string }> = [
  { id: 'dashboard', label: 'Dashboard' },
  { id: 'planning', label: 'Planning' },
  { id: 'openOrders', label: 'Customer Open Orders' },
  { id: 'componentOrders', label: 'Component Orders' },
  { id: 'longTermShortages', label: 'Workspace Shortages' },
  { id: 'finishedGoods', label: 'Finished Goods' },
  { id: 'workspaceShipments', label: 'Workspace Shipments' },
];

function formatTimestamp(value: string | null | undefined): string {
  if (!value) return 'Never';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Never' : date.toLocaleString();
}

export const OPEN_ORDERS_UNSAVED_MESSAGE = 'Unsaved Customer Open Orders planning changes will be discarded. Continue?';
export const PLANNING_UNSAVED_MESSAGE = 'Unsaved Planning adjustments will be discarded. Continue?';

function unsavedMessage(openOrders: boolean, planning: boolean): string | undefined {
  if (openOrders && planning) return 'Unsaved Customer Open Orders planning changes and unsaved Planning adjustments will be discarded. Continue?';
  if (planning) return PLANNING_UNSAVED_MESSAGE;
  return openOrders ? OPEN_ORDERS_UNSAVED_MESSAGE : undefined;
}

export function CustomerWorkspace({ workspace, onPlanningDirtyChange, confirmDiscard }: {
  workspace: WorkspaceAssignmentDto;
  onPlanningDirtyChange?: (dirty: boolean, message?: string) => void;
  /** In-app discard confirmation supplied by the shell. Without it (isolated rendering) the browser confirm is used. */
  confirmDiscard?: (message: string) => Promise<boolean>;
}) {
  const [activeModule, setActiveModule] = useState<CustomerModule>('dashboard');
  const [planningDirty, setPlanningDirty] = useState(false);
  const mps = useMpsDashboard(workspace.assignmentId);
  const snapshotId = mps.dashboard?.snapshot.snapshotId ?? null;
  // Stage 14.5: Planning proposals live here so they survive module switches (for example reloading
  // MPS on the Dashboard tab) and are reapplied to a replaced MPS snapshot.
  const proposals = usePlanningProposals(workspace.assignmentId, snapshotId);
  const planningDirtyChanged = useCallback((dirty: boolean) => setPlanningDirty(dirty), []);
  const title = workspace.displayName ?? workspace.site;

  // Unsaved-change protection reported to the shell (workspace switch, General, archive/delete/reset).
  const anyDirty = planningDirty || proposals.dirty;
  const dirtyMessage = unsavedMessage(planningDirty, proposals.dirty);
  useEffect(() => { onPlanningDirtyChange?.(anyDirty, dirtyMessage); }, [anyDirty, dirtyMessage, onPlanningDirtyChange]);
  useEffect(() => () => { onPlanningDirtyChange?.(false); }, [onPlanningDirtyChange]);

  return (
    <div className="customer-workspace">
      <header className="customer-workspace__header">
        <div>
          <h2 className="customer-workspace__title">{title}</h2>
          <div className="customer-workspace__meta">
            Active Parts: <strong data-testid="active-parts-count">{mps.dashboard?.snapshot.resolvedParentPartCount ?? '—'}</strong>
            {mps.dashboard && <>
              {' - '}{mps.dashboard.snapshot.status}{' - Last refresh: '}
              {formatTimestamp(mps.dashboard.snapshot.lastSuccessfulRefreshAtUtc)}
            </>}
          </div>
        </div>
      </header>

      <nav className="customer-workspace__modules" role="tablist" aria-label="Customer modules">
        {modules.map((module) => (
          <button
            key={module.id}
            type="button"
            role="tab"
            aria-selected={activeModule === module.id}
            className={`customer-workspace__module${activeModule === module.id ? ' customer-workspace__module--active' : ''}`}
            onClick={async () => {
              if (module.id !== 'openOrders' && planningDirty) {
                const proceed = confirmDiscard ? await confirmDiscard(OPEN_ORDERS_UNSAVED_MESSAGE) : window.confirm(OPEN_ORDERS_UNSAVED_MESSAGE);
                if (!proceed) return;
              }
              if (module.id !== 'openOrders') setPlanningDirty(false);
              setActiveModule(module.id);
            }}
          >
            {module.label}
          </button>
        ))}
      </nav>

      <div className="customer-workspace__content">
        <div hidden={activeModule !== 'dashboard'}>
          <MpsWorkspace
            active={activeModule === 'dashboard'}
            assignmentId={workspace.assignmentId}
            dashboard={mps.dashboard}
            dateBasis={mps.dateBasis}
            horizonWeeks={mps.horizonWeeks}
            isLoading={mps.isLoading}
            isRefreshing={mps.isRefreshing}
            error={mps.error}
            setDateBasis={mps.setDateBasis}
            setHorizonWeeks={mps.setHorizonWeeks}
            reload={mps.reload}
            refresh={mps.refresh}
          />
        </div>
        {activeModule === 'finishedGoods' && (
          <FinishedGoodsPanel assignmentId={workspace.assignmentId} snapshotId={snapshotId} site={workspace.site} />
        )}
        {activeModule === 'workspaceShipments' && <WorkspaceShipmentsPanel assignmentId={workspace.assignmentId} />}
        {activeModule === 'planning' && (
          <PlanningWorkbookPanel assignmentId={workspace.assignmentId} site={workspace.site}
            snapshotId={snapshotId} mpsLoading={mps.isLoading} proposals={proposals} />
        )}
        {activeModule === 'componentOrders' && (
          <ComponentOrdersPanel assignmentId={workspace.assignmentId} snapshotId={mps.dashboard?.snapshot.snapshotId ?? null} />
        )}
        {activeModule === 'openOrders' && (
          <OpenOrdersPanel assignmentId={workspace.assignmentId} snapshotId={mps.dashboard?.snapshot.snapshotId ?? null}
            workspaceName={workspace.displayName} site={workspace.site} onDirtyChange={planningDirtyChanged} />
        )}
        {activeModule === 'longTermShortages' && (
          <LongTermShortagesPanel assignmentId={workspace.assignmentId} snapshotId={mps.dashboard?.snapshot.snapshotId ?? null} site={workspace.site} />
        )}
      </div>
    </div>
  );
}

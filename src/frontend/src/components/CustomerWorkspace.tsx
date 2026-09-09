import { useState } from 'react';
import type { WorkspaceAssignmentDto } from '../api/client';
import { useMpsDashboard } from '../hooks/useMpsDashboard';
import { MpsWorkspace } from './MpsWorkspace';
import './CustomerWorkspace.css';

type CustomerModule = 'dashboard' | 'planning' | 'componentOrders' | 'finishedGoods';

const modules: ReadonlyArray<{ id: CustomerModule; label: string }> = [
  { id: 'dashboard', label: 'Dashboard' },
  { id: 'planning', label: 'Planning' },
  { id: 'componentOrders', label: 'Component Orders' },
  { id: 'finishedGoods', label: 'Finished Goods' },
];

const unavailableCopy: Record<Exclude<CustomerModule, 'dashboard'>, string> = {
  planning: 'Planning is not available yet. This module will be enabled when its accepted scheduling behavior is implemented.',
  componentOrders: 'Component Orders is not available yet. This Stage 10 workspace surface will be populated after its purchase-order data and workflow are accepted.',
  finishedGoods: 'Finished Goods is not available yet. This module will be enabled when its accepted scheduling behavior is implemented.',
};

function formatTimestamp(value: string | null | undefined): string {
  if (!value) return 'Never';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Never' : date.toLocaleString();
}

export function CustomerWorkspace({ workspace }: { workspace: WorkspaceAssignmentDto }) {
  const [activeModule, setActiveModule] = useState<CustomerModule>('dashboard');
  const mps = useMpsDashboard(workspace.assignmentId);
  const title = workspace.displayName ?? workspace.site;

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
            onClick={() => setActiveModule(module.id)}
          >
            {module.label}
          </button>
        ))}
      </nav>

      <div className="customer-workspace__content">
        <div hidden={activeModule !== 'dashboard'}>
          <MpsWorkspace
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
        {activeModule !== 'dashboard' && (
          <section className="customer-workspace__unavailable" aria-labelledby={`${activeModule}-heading`}>
            <h3 id={`${activeModule}-heading`}>{modules.find((module) => module.id === activeModule)?.label}</h3>
            <p>{unavailableCopy[activeModule]}</p>
          </section>
        )}
      </div>
    </div>
  );
}

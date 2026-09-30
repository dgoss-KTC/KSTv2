import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import type { WorkspaceAssignmentDto } from '../api/client';
import { CustomerWorkspace } from './CustomerWorkspace';

vi.mock('../hooks/useMpsDashboard', () => ({
  useMpsDashboard: () => ({ dashboard: null, dateBasis: 'dueDate', horizonWeeks: 4, isLoading: false, isRefreshing: false, error: null, setDateBasis: vi.fn(), setHorizonWeeks: vi.fn(), reload: vi.fn(), refresh: vi.fn() }),
}));
vi.mock('./MpsWorkspace', () => ({ MpsWorkspace: () => <div>MPS dashboard</div> }));
vi.mock('./ComponentOrdersPanel', () => ({ ComponentOrdersPanel: () => <div>Component Orders</div> }));
vi.mock('./LongTermShortagesPanel', () => ({ LongTermShortagesPanel: () => <div>Workspace Shortages panel</div> }));
vi.mock('./OpenOrdersPanel', () => ({ OpenOrdersPanel: ({ workspaceName, site }: { workspaceName: string | null; site: string }) =>
  <div>Open Orders for {workspaceName ?? site}</div> }));

const workspace: WorkspaceAssignmentDto = {
  assignmentId: 'ws-1', displayName: 'Line 1', site: 'NW', productLineFrom: null, productLineTo: null,
  parentParts: [], isTemporary: false, coverageEndsOn: null, isEnabled: true, sortOrder: 0,
};

describe('CustomerWorkspace', () => {
  it('labels the Stage 11-A workspace tab as Workspace Shortages', () => {
    render(<CustomerWorkspace workspace={workspace} />);

    const tab = screen.getByRole('tab', { name: 'Workspace Shortages' });
    expect(tab).toBeInTheDocument();
    fireEvent.click(tab);
    expect(screen.getByText('Workspace Shortages panel')).toBeInTheDocument();
    expect(screen.queryByRole('tab', { name: 'Long-Term Shortages' })).not.toBeInTheDocument();
  });

  it('places Customer Open Orders before Component Orders and passes the display name and site', () => {
    render(<CustomerWorkspace workspace={workspace} />);
    const tabs = screen.getAllByRole('tab');
    expect(tabs.findIndex(tab => tab.textContent === 'Customer Open Orders')).toBeLessThan(tabs.findIndex(tab => tab.textContent === 'Component Orders'));
    fireEvent.click(screen.getByRole('tab', { name: 'Customer Open Orders' }));
    expect(screen.getByText('Open Orders for Line 1')).toBeInTheDocument();
  });
});

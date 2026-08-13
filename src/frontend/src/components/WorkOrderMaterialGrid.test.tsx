import { describe, it, expect } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { WorkOrderMaterialGrid } from './WorkOrderMaterialGrid';
import type { WorkOrderMaterialLineDto } from '../api/client';

function makeLine(overrides: Partial<WorkOrderMaterialLineDto> = {}): WorkOrderMaterialLineDto {
  return {
    componentPart: 'COMP1',
    componentDescription: 'Fastener',
    requiredQuantity: 10,
    issuedQuantity: 10,
    varianceQuantity: 0,
    issuedPercent: 100,
    issueStatus: 'withinExpectedRange',
    isManufactured: false,
    isFullyIssued: true,
    ...overrides,
  };
}

describe('WorkOrderMaterialGrid', () => {
  it('renders columns and default exception-first sort order', () => {
    render(
      <WorkOrderMaterialGrid
        lines={[
          makeLine({ componentPart: 'NORMAL', issuedPercent: 98, issueStatus: 'withinExpectedRange' }),
          makeLine({ componentPart: 'UNDER', issuedPercent: 50, issueStatus: 'underIssuedException' }),
        ]}
        depth={1}
        woid="WO-1000"
        assignmentId="assignment-1"
        snapshotId="snapshot-1"
      />,
    );

    expect(screen.getByRole('columnheader', { name: 'Component' })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'Issued %' })).toBeInTheDocument();
    const rows = screen.getAllByRole('row').slice(1); // drop header row
    expect(within(rows[0]).getByText('UNDER')).toBeInTheDocument();
    expect(within(rows[1]).getByText('NORMAL')).toBeInTheDocument();
  });

  it('applies exception styling to Issued % outside 95-105 but not within range', () => {
    render(
      <WorkOrderMaterialGrid
        lines={[
          makeLine({ componentPart: 'NORMAL', issuedPercent: 100, issueStatus: 'withinExpectedRange' }),
          makeLine({ componentPart: 'OVER', issuedPercent: 160, issueStatus: 'overIssuedException' }),
        ]}
        depth={1}
        woid="WO-1000"
        assignmentId="assignment-1"
        snapshotId="snapshot-1"
      />,
    );

    expect(screen.getByText('160%')).toHaveClass('work-order-material-grid__issued-pct--exception');
    expect(screen.getByText('100%')).not.toHaveClass('work-order-material-grid__issued-pct--exception');
  });

  it('gives manufactured rows a distinct treatment and a chevron affordance', () => {
    render(
      <WorkOrderMaterialGrid
        lines={[
          makeLine({ componentPart: 'MADE', isManufactured: true }),
          makeLine({ componentPart: 'BOUGHT', isManufactured: false }),
        ]}
        depth={1}
        woid="WO-1000"
        assignmentId="assignment-1"
        snapshotId="snapshot-1"
      />,
    );

    const madeRow = screen.getByText('MADE').closest('tr');
    const boughtRow = screen.getByText('BOUGHT').closest('tr');
    expect(madeRow).toHaveClass('work-order-material-grid__row--manufactured');
    expect(madeRow).toHaveClass('work-order-material-grid__row--drillable');
    expect(boughtRow).not.toHaveClass('work-order-material-grid__row--manufactured');
    expect(madeRow?.querySelector('.work-order-material-grid__chevron')).not.toBeNull();
    expect(boughtRow?.querySelector('.work-order-material-grid__chevron')).toBeNull();
  });

  it('applies both manufactured and exception styling together on a manufactured line with variance', () => {
    render(
      <WorkOrderMaterialGrid
        lines={[
          makeLine({ componentPart: 'MADE-OVER', isManufactured: true, issuedPercent: 160, issueStatus: 'overIssuedException' }),
        ]}
        depth={1}
        woid="WO-1000"
        assignmentId="assignment-1"
        snapshotId="snapshot-1"
      />,
    );

    const row = screen.getByText('MADE-OVER').closest('tr');
    expect(row).toHaveClass('work-order-material-grid__row--manufactured');
    expect(row).toHaveClass('work-order-material-grid__row--drillable');
    expect(screen.getByText('160%')).toHaveClass('work-order-material-grid__issued-pct--exception');
    expect(row?.querySelector('.work-order-material-grid__chevron')).not.toBeNull();
  });

  it('disables the drill affordance at the maximum drill depth while keeping the manufactured indicator', () => {
    render(
      <WorkOrderMaterialGrid
        lines={[makeLine({ componentPart: 'MADE', isManufactured: true })]}
        depth={3}
        woid="WO-1000"
        assignmentId="assignment-1"
        snapshotId="snapshot-1"
      />,
    );

    const madeRow = screen.getByText('MADE').closest('tr');
    expect(madeRow).toHaveClass('work-order-material-grid__row--manufactured');
    expect(madeRow).not.toHaveClass('work-order-material-grid__row--drillable');
    expect(madeRow?.querySelector('.work-order-material-grid__chevron--disabled')).not.toBeNull();
  });

  it('filters by Part Number, case-insensitively and partially, with an easy clear', async () => {
    const user = userEvent.setup();
    render(
      <WorkOrderMaterialGrid
        lines={[makeLine({ componentPart: 'ABC-100' }), makeLine({ componentPart: 'xyz-200' })]}
        depth={1}
        woid="WO-1000"
        assignmentId="assignment-1"
        snapshotId="snapshot-1"
      />,
    );

    await user.type(screen.getByLabelText(/filter by part number/i), 'abc');
    expect(screen.getByText('ABC-100')).toBeInTheDocument();
    expect(screen.queryByText('xyz-200')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /clear/i }));
    expect(screen.getByText('ABC-100')).toBeInTheDocument();
    expect(screen.getByText('xyz-200')).toBeInTheDocument();
  });

  it('shows a deliberate no-match message rather than an empty table when the filter matches nothing', async () => {
    const user = userEvent.setup();
    render(
      <WorkOrderMaterialGrid
        lines={[makeLine({ componentPart: 'ABC-100' })]}
        depth={1}
        woid="WO-1000"
        assignmentId="assignment-1"
        snapshotId="snapshot-1"
      />,
    );

    await user.type(screen.getByLabelText(/filter by part number/i), 'zzz');
    expect(screen.getByText(/no material lines match/i)).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });
});

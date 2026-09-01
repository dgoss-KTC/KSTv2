import { describe, expect, it } from 'vitest';
import { render, within } from '@testing-library/react';
import type { WorkOrderSummaryDto } from '../api/client';
import { WorkOrderCard } from './WorkOrderCard';

const workOrder: WorkOrderSummaryDto = {
  partNumber: '9300-4052',
  woid: '33334396',
  status: 'released',
  orderedQuantity: 1440,
  completedQuantity: 0,
  openQuantity: 1440,
  releaseDate: '2026-09-01',
  dueDate: '2026-09-07',
  salesOrder: null,
  kitting: {
    applicableLineCount: 2,
    fullyIssuedLineCount: 0,
    kittingPercent: 0,
  },
};

describe('WorkOrderCard', () => {
  it('keeps Release and Due together in a separate row from quantities', () => {
    render(<WorkOrderCard workOrder={workOrder} assignmentId="assignment-1" snapshotId="snapshot-1" dateBasis="dueDate" />);

    const quantityFields = document.querySelector<HTMLElement>('.work-order-card__quantity-fields');
    const dateFields = document.querySelector<HTMLElement>('.work-order-card__date-fields');

    expect(quantityFields).not.toBeNull();
    expect(dateFields).not.toBeNull();
    if (quantityFields === null || dateFields === null) throw new Error('Expected Work Order field rows.');

    expect(within(quantityFields).getByText('Ordered')).toBeInTheDocument();
    expect(within(quantityFields).getByText('Completed')).toBeInTheDocument();
    expect(within(quantityFields).getByText('Open')).toBeInTheDocument();
    expect(within(dateFields).getByText('Release')).toBeInTheDocument();
    expect(within(dateFields).getByText('Due')).toBeInTheDocument();
    expect(within(dateFields).getByText('Sep 1')).toBeInTheDocument();
    expect(within(dateFields).getByText('Sep 7')).toBeInTheDocument();
  });
});

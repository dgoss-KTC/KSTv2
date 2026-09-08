import { describe, expect, it } from 'vitest';
import { render, screen, within } from '@testing-library/react';
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
    render(<WorkOrderCard workOrder={workOrder} />);

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

  it.each([
    ['shortage', 'Has Shortage'],
    ['dataIssue', 'Unknown / Data Issue'],
    ['clear', 'No current Shortage'],
    ['unavailable', 'Shortage summary unavailable'],
  ] as const)('renders the %s shortage state label and card class', (shortageState, label) => {
    render(<WorkOrderCard workOrder={workOrder} shortageState={shortageState} />);

    expect(screen.getByText(label)).toBeInTheDocument();
    expect(screen.getByRole('listitem')).toHaveClass(`work-order-card--${shortageState}`);
  });
});

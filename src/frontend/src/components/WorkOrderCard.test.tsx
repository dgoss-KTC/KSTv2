import { describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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
  it('has one whole-card button, operable by Enter and Space without nested controls', async () => {
    const user = userEvent.setup(); const toggle = vi.fn();
    const view = render(<WorkOrderCard workOrder={workOrder} onToggle={toggle} />);
    const button = screen.getByRole('button');
    expect(button.querySelector('button, input, a, label')).toBeNull();
    button.focus(); await user.keyboard('{Enter}'); await user.keyboard(' ');
    expect(toggle).toHaveBeenCalledTimes(2); expect(toggle).toHaveBeenLastCalledWith(button);
    expect(button).toHaveAttribute('aria-expanded', 'false');
    view.rerender(<WorkOrderCard workOrder={workOrder} isOpen onToggle={toggle} />);
    expect(button).toHaveAttribute('aria-expanded', 'true');
    expect(button).toHaveTextContent('Material shown below');
  });
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
    ['dataIssue', 'Unable to confirm availability'],
    ['clear', 'No current Shortage'],
    ['unavailable', 'Shortage summary unavailable'],
  ] as const)('renders the %s shortage state label and card class', (shortageState, label) => {
    render(<WorkOrderCard workOrder={workOrder} shortageState={shortageState} />);

    expect(screen.getByText(label)).toBeInTheDocument();
    expect(screen.getByRole('listitem')).toHaveClass(`work-order-card--${shortageState}`);
  });
});

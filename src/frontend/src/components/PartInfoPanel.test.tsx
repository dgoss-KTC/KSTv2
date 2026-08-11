import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { PartInfoPanel } from './PartInfoPanel';
import type { PartDetailResponseDto } from '../api/client';

function makeDetail(overrides: Partial<PartDetailResponseDto> = {}): PartDetailResponseDto {
  return {
    site: 'NW',
    partNumber: 'ABC100',
    plannerCode: 'JSMITH',
    manufacturingLeadTimeDays: 10,
    safetyTimeDays: 2,
    partStatusCode: 'C',
    partStatusDescription: 'CURRENT',
    currentRevision: 'B',
    description: 'Widget Assembly',
    iosCode: '1234',
    safetyStockQuantity: 250,
    quantityOnHand: 1325,
    quantityNonNet: 75,
    quantityRmaOnHand: 25,
    priceBreaks: [{ minimumOrderQuantity: 100, unitPrice: 12.45 }],
    loadedAtUtc: '2026-08-10T22:30:00Z',
    isStale: false,
    warning: null,
    ...overrides,
  };
}

describe('PartInfoPanel', () => {
  it('renders a loading state', () => {
    render(
      <PartInfoPanel
        partNumber="ABC100"
        detail={null}
        isLoading={true}
        error={null}
        onRetry={vi.fn()}
        onBack={vi.fn()}
      />,
    );

    expect(screen.getByText(/loading part information/i)).toBeInTheDocument();
  });

  it('renders a missing-part message', () => {
    render(
      <PartInfoPanel
        partNumber="ABC100"
        detail={null}
        isLoading={false}
        error={{ type: 'missing-part' }}
        onRetry={vi.fn()}
        onBack={vi.fn()}
      />,
    );

    expect(screen.getByText(/no qad part master record was found for abc100/i)).toBeInTheDocument();
  });

  it('renders a generic error message with a retry button that invokes onRetry', async () => {
    const user = userEvent.setup();
    const onRetry = vi.fn();
    render(
      <PartInfoPanel
        partNumber="ABC100"
        detail={null}
        isLoading={false}
        error={{ type: 'error', detail: 'Database currently unavailable.' }}
        onRetry={onRetry}
        onBack={vi.fn()}
      />,
    );

    expect(screen.getByText('Database currently unavailable.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /retry/i }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it('invokes onBack when Back to full grid is clicked', async () => {
    const user = userEvent.setup();
    const onBack = vi.fn();
    render(
      <PartInfoPanel
        partNumber="ABC100"
        detail={makeDetail()}
        isLoading={false}
        error={null}
        onRetry={vi.fn()}
        onBack={onBack}
      />,
    );

    await user.click(screen.getByRole('button', { name: /back to full grid/i }));
    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it('renders all loaded fields', () => {
    render(
      <PartInfoPanel
        partNumber="ABC100"
        detail={makeDetail()}
        isLoading={false}
        error={null}
        onRetry={vi.fn()}
        onBack={vi.fn()}
      />,
    );

    expect(screen.getByText('10 days')).toBeInTheDocument();
    expect(screen.getByText('2 days')).toBeInTheDocument();
    expect(screen.getByText(/C.*CURRENT/)).toBeInTheDocument();
    expect(screen.getByText('JSMITH')).toBeInTheDocument();
    expect(screen.getByText('B')).toBeInTheDocument();
    expect(screen.getByText('Widget Assembly')).toBeInTheDocument();
    expect(screen.getByText('1234')).toBeInTheDocument();
    expect(screen.getByText('250')).toBeInTheDocument();
    expect(screen.getByText('1,325')).toBeInTheDocument();
    expect(screen.getByText('75')).toBeInTheDocument();
    expect(screen.getByText('25')).toBeInTheDocument();
  });

  it('renders RMA On Hand quantity', () => {
    render(
      <PartInfoPanel
        partNumber="ABC100"
        detail={makeDetail({ quantityRmaOnHand: 3 })}
        isLoading={false}
        error={null}
        onRetry={vi.fn()}
        onBack={vi.fn()}
      />,
    );

    expect(screen.getByText('RMA On Hand')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
  });

  it('renders zero RMA On Hand as 0, not blank', () => {
    render(
      <PartInfoPanel
        partNumber="ABC100"
        detail={makeDetail({ quantityRmaOnHand: 0 })}
        isLoading={false}
        error={null}
        onRetry={vi.fn()}
        onBack={vi.fn()}
      />,
    );

    expect(screen.getByText('RMA On Hand')).toBeInTheDocument();
    expect(screen.getByText('0')).toBeInTheDocument();
  });

  it('falls back to \u2014 for null/blank optional fields', () => {
    render(
      <PartInfoPanel
        partNumber="ABC100"
        detail={makeDetail({
          plannerCode: null,
          manufacturingLeadTimeDays: null,
          safetyTimeDays: null,
          partStatusCode: null,
          partStatusDescription: null,
          currentRevision: null,
          description: null,
          iosCode: null,
          safetyStockQuantity: null,
        })}
        isLoading={false}
        error={null}
        onRetry={vi.fn()}
        onBack={vi.fn()}
      />,
    );

    expect(screen.getAllByText('\u2014').length).toBeGreaterThanOrEqual(8);
  });

  it('shows a stale banner with the warning message when isStale is true', () => {
    render(
      <PartInfoPanel
        partNumber="ABC100"
        detail={makeDetail({ isStale: true, warning: 'A newer refresh could not be completed.' })}
        isLoading={false}
        error={null}
        onRetry={vi.fn()}
        onBack={vi.fn()}
      />,
    );

    expect(screen.getByRole('alert')).toHaveTextContent('A newer refresh could not be completed.');
  });

  it('shows "No Data Found" when there are no price breaks', () => {
    render(
      <PartInfoPanel
        partNumber="ABC100"
        detail={makeDetail({ priceBreaks: [] })}
        isLoading={false}
        error={null}
        onRetry={vi.fn()}
        onBack={vi.fn()}
      />,
    );

    expect(screen.getByText('No Data Found')).toBeInTheDocument();
  });

  it('shows a compact single line when there is exactly one price tier', () => {
    render(
      <PartInfoPanel
        partNumber="ABC100"
        detail={makeDetail({ priceBreaks: [{ minimumOrderQuantity: 100, unitPrice: 12.45 }] })}
        isLoading={false}
        error={null}
        onRetry={vi.fn()}
        onBack={vi.fn()}
      />,
    );

    expect(screen.getByText('100 @ $12.45')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('shows a table when there are multiple price tiers', () => {
    render(
      <PartInfoPanel
        partNumber="ABC100"
        detail={makeDetail({
          priceBreaks: [
            { minimumOrderQuantity: 100, unitPrice: 12.45 },
            { minimumOrderQuantity: 500, unitPrice: 9.99 },
          ],
        })}
        isLoading={false}
        error={null}
        onRetry={vi.fn()}
        onBack={vi.fn()}
      />,
    );

    const table = screen.getByRole('table');
    expect(table).toBeInTheDocument();
    expect(screen.getByText('$12.45')).toBeInTheDocument();
    expect(screen.getByText('$9.99')).toBeInTheDocument();
  });
});

import { describe, it, expect, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BomPanel } from './BomPanel';
import type { BomLineDto, BomResponseDto } from '../api/client';

function makeBomLine(overrides: Partial<BomLineDto> = {}): BomLineDto {
  return {
    occurrenceKey: 'ok-1',
    level: 1,
    componentPart: 'COMP-1',
    pmCode: 'P',
    isPhantom: false,
    description: 'Component One',
    quantityPer: 2,
    scrapPercentage: 0.5,
    netQuantityOnHand: 10,
    nonNetQuantityOnHand: 3,
    ...overrides,
  };
}

function makeBom(overrides: Partial<BomResponseDto> = {}): BomResponseDto {
  return {
    site: 'SW',
    parentPart: 'PARENT-1',
    effectiveDate: '2026-08-13',
    lines: [makeBomLine()],
    loadedAtUtc: '2026-08-13T12:00:00Z',
    isStale: false,
    warning: null,
    ...overrides,
  };
}

function renderPanel(props: Partial<React.ComponentProps<typeof BomPanel>> = {}) {
  return render(
    <BomPanel
      parentPart="PARENT-1"
      bom={makeBom()}
      isLoading={false}
      error={null}
      onRetry={vi.fn()}
      {...props}
    />,
  );
}

function bodyRowParts(): string[][] {
  const rows = within(screen.getByRole('table')).getAllByRole('row').slice(1);
  return rows.map((row) => within(row).getAllByRole('cell').map((cell) => cell.textContent ?? ''));
}

describe('BomPanel', () => {
  it('renders the accepted nine columns in the exact order', () => {
    renderPanel();
    const headers = screen.getAllByRole('columnheader').map((h) => h.textContent);
    expect(headers).toEqual([
      'Level',
      'Component Item',
      'P/M',
      'Phantom',
      'Description',
      'Qty Per',
      'Scrap',
      'Net QOH',
      'Non-Net QOH',
    ]);
  });

  it('preserves the API row order (no sorting)', () => {
    renderPanel({
      bom: makeBom({
        lines: [
          makeBomLine({ occurrenceKey: 'ok-b', componentPart: 'B-2' }),
          makeBomLine({ occurrenceKey: 'ok-a', componentPart: 'A-1' }),
          makeBomLine({ occurrenceKey: 'ok-c', componentPart: 'C-3' }),
        ],
      }),
    });
    expect(bodyRowParts().map((row) => row[1])).toEqual(['B-2', 'A-1', 'C-3']);
  });

  it('displays actual Level values unchanged, including gaps', () => {
    renderPanel({
      bom: makeBom({
        lines: [
          makeBomLine({ occurrenceKey: 'ok-1', level: 1 }),
          makeBomLine({ occurrenceKey: 'ok-2', level: 3 }),
        ],
      }),
    });
    expect(bodyRowParts().map((row) => row[0])).toEqual(['1', '3']);
  });

  it('keeps repeated component occurrences as separate rows', () => {
    renderPanel({
      bom: makeBom({
        lines: [
          makeBomLine({ occurrenceKey: 'ok-1', componentPart: 'DUP-1', netQuantityOnHand: 5 }),
          makeBomLine({ occurrenceKey: 'ok-2', componentPart: 'MID-1', level: 2 }),
          makeBomLine({ occurrenceKey: 'ok-3', componentPart: 'DUP-1', netQuantityOnHand: 5 }),
        ],
      }),
    });
    const parts = bodyRowParts().map((row) => row[1]);
    expect(parts).toEqual(['DUP-1', 'MID-1', 'DUP-1']);
  });

  it('displays P/M, Phantom, quantities, and inventory from the contract', () => {
    renderPanel({
      bom: makeBom({
        lines: [
          makeBomLine({
            pmCode: 'M',
            isPhantom: true,
            quantityPer: 4,
            scrapPercentage: 2,
            netQuantityOnHand: 1234.5,
            nonNetQuantityOnHand: 7,
          }),
        ],
      }),
    });
    expect(bodyRowParts()[0]).toEqual(['1', 'COMP-1', 'M', 'Yes', 'Component One', '4', '2%', '1,234.5', '7']);
  });

  it('does not render RMA or requirement/MRP columns', () => {
    renderPanel();
    expect(screen.queryByText(/rma/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/extended requirement/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/coverage/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/projected/i)).not.toBeInTheDocument();
  });

  it('falls back to \u2014 for null optional fields', () => {
    renderPanel({
      bom: makeBom({
        lines: [
          makeBomLine({
            description: null,
            pmCode: null,
            quantityPer: null,
            scrapPercentage: null,
          }),
        ],
      }),
    });
    expect(bodyRowParts()[0]).toEqual(['1', 'COMP-1', '\u2014', 'No', '\u2014', '\u2014', '\u2014', '10', '3']);
  });

  describe('search', () => {
    it('matches Component Part substrings case-insensitively', async () => {
      const user = userEvent.setup();
      renderPanel({
        bom: makeBom({
          lines: [
            makeBomLine({ occurrenceKey: 'ok-1', componentPart: 'ABC-100' }),
            makeBomLine({ occurrenceKey: 'ok-2', componentPart: 'XYZ-200' }),
          ],
        }),
      });
      await user.type(screen.getByLabelText('Filter by Component Item'), 'abc-1');
      expect(bodyRowParts().map((row) => row[1])).toEqual(['ABC-100']);
    });

    it('does not match description-only text', async () => {
      const user = userEvent.setup();
      renderPanel({
        bom: makeBom({
          lines: [makeBomLine({ componentPart: 'ABC-100', description: 'Widget Fastener' })],
        }),
      });
      await user.type(screen.getByLabelText('Filter by Component Item'), 'fastener');
      expect(screen.getByText('No BOM components match the current filters.')).toBeInTheDocument();
      expect(screen.queryByRole('table')).not.toBeInTheDocument();
    });

    it('preserves relative structural order and repeated matches', async () => {
      const user = userEvent.setup();
      renderPanel({
        bom: makeBom({
          lines: [
            makeBomLine({ occurrenceKey: 'ok-1', componentPart: 'SUB-1' }),
            makeBomLine({ occurrenceKey: 'ok-2', componentPart: 'OTHER-1' }),
            makeBomLine({ occurrenceKey: 'ok-3', componentPart: 'SUB-1', level: 2 }),
            makeBomLine({ occurrenceKey: 'ok-4', componentPart: 'SUB-2' }),
          ],
        }),
      });
      await user.type(screen.getByLabelText('Filter by Component Item'), 'sub');
      expect(bodyRowParts().map((row) => row[1])).toEqual(['SUB-1', 'SUB-1', 'SUB-2']);
    });

    it('clearing the search restores the complete sequence', async () => {
      const user = userEvent.setup();
      renderPanel({
        bom: makeBom({
          lines: [
            makeBomLine({ occurrenceKey: 'ok-1', componentPart: 'SUB-1' }),
            makeBomLine({ occurrenceKey: 'ok-2', componentPart: 'OTHER-1' }),
          ],
        }),
      });
      const input = screen.getByLabelText('Filter by Component Item');
      await user.type(input, 'sub');
      expect(bodyRowParts()).toHaveLength(1);
      await user.click(screen.getByRole('button', { name: 'Clear' }));
      expect(input).toHaveValue('');
      expect(bodyRowParts().map((row) => row[1])).toEqual(['SUB-1', 'OTHER-1']);
    });
  });

  describe('P/M and Phantom filters', () => {
    // Order deliberately not alphabetical; includes a repeated component (SUB-1) and a level
    // gap (1 → 3) so filtering can be checked against structural truth.
    const filterLines: BomLineDto[] = [
      makeBomLine({ occurrenceKey: 'f-1', level: 1, componentPart: 'SUB-1', pmCode: 'P', isPhantom: false }),
      makeBomLine({ occurrenceKey: 'f-2', level: 1, componentPart: 'SUB-2', pmCode: 'M', isPhantom: true }),
      makeBomLine({ occurrenceKey: 'f-3', level: 3, componentPart: 'SUB-1', pmCode: 'P', isPhantom: false }),
      makeBomLine({ occurrenceKey: 'f-4', level: 3, componentPart: 'SUB-3', pmCode: 'M', isPhantom: false }),
    ];

    it('default All displays P and M rows', () => {
      renderPanel({ bom: makeBom({ lines: filterLines }) });
      expect(bodyRowParts().map((row) => row[1])).toEqual(['SUB-1', 'SUB-2', 'SUB-1', 'SUB-3']);
    });

    it('P displays only P rows, in original order', async () => {
      const user = userEvent.setup();
      renderPanel({ bom: makeBom({ lines: filterLines }) });
      await user.selectOptions(screen.getByLabelText('P/M'), 'P');
      expect(bodyRowParts().map((row) => row[1])).toEqual(['SUB-1', 'SUB-1']);
    });

    it('M displays only M rows, in original order', async () => {
      const user = userEvent.setup();
      renderPanel({ bom: makeBom({ lines: filterLines }) });
      await user.selectOptions(screen.getByLabelText('P/M'), 'M');
      expect(bodyRowParts().map((row) => row[1])).toEqual(['SUB-2', 'SUB-3']);
    });

    it('default All displays phantom and non-phantom rows', () => {
      renderPanel({ bom: makeBom({ lines: filterLines }) });
      expect(bodyRowParts().map((row) => row[3])).toEqual(['No', 'Yes', 'No', 'No']);
    });

    it('Yes displays only phantom rows', async () => {
      const user = userEvent.setup();
      renderPanel({ bom: makeBom({ lines: filterLines }) });
      await user.selectOptions(screen.getByLabelText('Phantom'), 'yes');
      expect(bodyRowParts().map((row) => row[1])).toEqual(['SUB-2']);
    });

    it('No displays only non-phantom rows', async () => {
      const user = userEvent.setup();
      renderPanel({ bom: makeBom({ lines: filterLines }) });
      await user.selectOptions(screen.getByLabelText('Phantom'), 'no');
      expect(bodyRowParts().map((row) => row[1])).toEqual(['SUB-1', 'SUB-1', 'SUB-3']);
    });

    it('search, P/M, and Phantom combine with AND semantics, preserving order, duplicates, and level gaps', async () => {
      const user = userEvent.setup();
      renderPanel({ bom: makeBom({ lines: filterLines }) });
      await user.type(screen.getByLabelText('Filter by Component Item'), 'sub');
      await user.selectOptions(screen.getByLabelText('P/M'), 'M');
      await user.selectOptions(screen.getByLabelText('Phantom'), 'yes');
      // Only f-2 satisfies all three (SUB-2, M, phantom).
      expect(bodyRowParts()).toEqual([['1', 'SUB-2', 'M', 'Yes', 'Component One', '2', '0.5%', '10', '3']]);

      // Widen to P + all phantoms: the repeated SUB-1 occurrences stay repeated, in order, with
      // the 1 → 3 level gap intact.
      await user.selectOptions(screen.getByLabelText('P/M'), 'P');
      await user.selectOptions(screen.getByLabelText('Phantom'), 'all');
      expect(bodyRowParts().map((row) => row[1])).toEqual(['SUB-1', 'SUB-1']);
      expect(bodyRowParts().map((row) => row[0])).toEqual(['1', '3']);
    });

    it('Clear resets search, P/M, and Phantom and restores the complete sequence', async () => {
      const user = userEvent.setup();
      renderPanel({ bom: makeBom({ lines: filterLines }) });
      const input = screen.getByLabelText('Filter by Component Item');
      const pmSelect = screen.getByLabelText('P/M');
      const phantomSelect = screen.getByLabelText('Phantom');

      await user.type(input, 'sub');
      await user.selectOptions(pmSelect, 'M');
      await user.selectOptions(phantomSelect, 'yes');
      expect(bodyRowParts()).toHaveLength(1);

      await user.click(screen.getByRole('button', { name: 'Clear' }));
      expect(input).toHaveValue('');
      expect(pmSelect).toHaveValue('all');
      expect(phantomSelect).toHaveValue('all');
      expect(bodyRowParts().map((row) => row[1])).toEqual(['SUB-1', 'SUB-2', 'SUB-1', 'SUB-3']);
      // No clear action remains once every filter is back to its default.
      expect(screen.queryByRole('button', { name: 'Clear' })).not.toBeInTheDocument();
    });

    it('does not show the clear action while all filters are at their defaults', () => {
      renderPanel({ bom: makeBom({ lines: filterLines }) });
      expect(screen.queryByRole('button', { name: 'Clear' })).not.toBeInTheDocument();
    });

    it('a non-empty BOM whose filters match zero rows uses the filtered-zero message (not the true-empty one)', async () => {
      const user = userEvent.setup();
      renderPanel({ bom: makeBom({ lines: filterLines }) });
      await user.type(screen.getByLabelText('Filter by Component Item'), 'zzz');
      await user.selectOptions(screen.getByLabelText('P/M'), 'M');
      expect(screen.getByText('No BOM components match the current filters.')).toBeInTheDocument();
      expect(screen.queryByText(/no bom components found/i)).not.toBeInTheDocument();
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
      expect(screen.queryByRole('table')).not.toBeInTheDocument();
    });
  });

  it('renders a loading state without an empty-grid message', () => {
    renderPanel({ bom: null, isLoading: true });
    expect(screen.getByText(/loading bom/i)).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
    expect(screen.queryByText(/no bom components/i)).not.toBeInTheDocument();
  });

  it('renders a deliberate non-error empty state for a successful empty BOM', () => {
    renderPanel({ bom: makeBom({ lines: [] }) });
    expect(screen.getByText('No BOM components found for PARENT-1.')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('renders the error detail with a Retry button that invokes onRetry', async () => {
    const user = userEvent.setup();
    const onRetry = vi.fn();
    renderPanel({
      bom: null,
      isLoading: false,
      error: { type: 'error', detail: 'Database currently unavailable.' },
      onRetry,
    });
    expect(screen.getByText('Database currently unavailable.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /retry/i }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it('shows a stale banner with the backend warning message when isStale is true', () => {
    renderPanel({
      bom: makeBom({ isStale: true, warning: 'Showing the last known BOM information. A newer refresh could not be completed.' }),
    });
    expect(screen.getByRole('alert')).toHaveTextContent('A newer refresh could not be completed.');
    expect(screen.getByRole('table')).toBeInTheDocument();
  });
});

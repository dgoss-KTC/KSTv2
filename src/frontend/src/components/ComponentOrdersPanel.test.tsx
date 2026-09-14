import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ApiError } from '../api/client';
import type { ComponentOrderLine, ComponentOrdersResponse } from '../api/componentOrdersApi';
import { fetchComponentOrders } from '../api/componentOrdersApi';
import { ComponentOrdersPanel } from './ComponentOrdersPanel';

const fetchMock = vi.fn();
vi.mock('../api/componentOrdersApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/componentOrdersApi')>()),
  fetchComponentOrders: (...args: Parameters<typeof fetchComponentOrders>) => fetchMock(...args),
}));

function line(overrides: Partial<ComponentOrderLine> = {}): ComponentOrderLine {
  return {
    componentPart: 'COMP',
    description: 'Hex bolt M8',
    leadTimeDays: null,
    poNumber: '2076185',
    poLine: 1,
    dueDate: null,
    openQuantity: 10,
    confirmed: true,
    supplierDisplay: 'ACME Industrial',
    buyerDisplay: null,
    manufacturerItem: null,
    isKss: false,
    trackingInfo: null,
    isCreditHold: null,
    isCia: null,
    currentComments: null,
    ...overrides,
  };
}

function response(groups: ComponentOrdersResponse['groups'], enrichmentAvailability = 'Available'): ComponentOrdersResponse {
  return { snapshotId: 'snap-1', enrichmentAvailability, groups };
}

beforeEach(() => {
  fetchMock.mockReset();
});

describe('ComponentOrdersPanel states', () => {
  it('shows a neutral load-first state when no MPS snapshot is available yet', () => {
    render(<ComponentOrdersPanel assignmentId="ws-1" snapshotId={null} />);
    expect(screen.getByText(/Load the MPS dashboard before viewing Component Orders/)).toBeTruthy();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('shows a loading state while fetching', async () => {
    fetchMock.mockReturnValue(new Promise(() => {})); // never settles
    render(<ComponentOrdersPanel assignmentId="ws-1" snapshotId="snap-1" />);
    expect(await screen.findByText(/Loading component orders/)).toBeTruthy();
  });

  it('maps a stale (409) failure to a refresh prompt with retry', async () => {
    fetchMock.mockRejectedValue(new ApiError(409, 'http://test/component-orders', '{"title":"Snapshot changed","detail":"This workspace\'s MPS snapshot has changed since the requested snapshot id was shown. Refresh and retry."}'));
    render(<ComponentOrdersPanel assignmentId="ws-1" snapshotId="snap-1" />);

    expect(await screen.findByText(/out of date/)).toBeTruthy();
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
  });

  it('shows the empty state when no component has qualifying open PO lines', async () => {
    fetchMock.mockResolvedValue(response([]));
    render(<ComponentOrdersPanel assignmentId="ws-1" snapshotId="snap-1" />);
    expect(await screen.findByText(/No components have qualifying open PO lines/)).toBeTruthy();
  });
});

describe('ComponentOrdersPanel grouping and display', () => {
  it('renders one collapsed row per component with the earliest line, an expand arrow only when more lines exist, and child rows repeating the component number', async () => {
    fetchMock.mockResolvedValue(response([
      {
        componentPart: 'COMP-A',
        displayLine: line({ poNumber: '100', dueDate: null }), // missing earliest due → yellow exception row first
        additionalLines: [line({ poNumber: '200', poLine: 4, dueDate: '2999-01-01' })],
      },
      {
        componentPart: 'COMP-B',
        displayLine: line({ componentPart: 'COMP-B', poNumber: '300', dueDate: '2026-01-05' }), // already past → late
        additionalLines: [],
      },
    ]));

    render(<ComponentOrdersPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const table = await screen.findByRole('table');

    const bodyRows = within(table).getAllByRole('row').slice(1);
    expect(bodyRows).toHaveLength(2); // collapsed rows only until expanded

    const [collapsedA, collapsedB] = bodyRows;
    // Expand arrow exists beside the component number only for COMP-A.
    expect(within(collapsedA).getByRole('button', { name: /Expand COMP-A/ })).toBeTruthy();
    expect(within(collapsedB).queryByRole('button')).toBeNull();

    // Collapsed rows show their display line values.
    expect(within(collapsedA).getAllByText('COMP-A').length).toBeGreaterThan(0);
    expect(within(collapsedA).getByText('100')).toBeTruthy();
    expect(within(collapsedB).getByText('300')).toBeTruthy();

    // Expand COMP-A: child rows repeat the component number with their own line values.
    fireEvent.click(within(collapsedA).getByRole('button', { name: /Expand COMP-A/ }));
    const expandedRows = within(table).getAllByRole('row').slice(1);
    expect(expandedRows).toHaveLength(3);
    const childRow = expandedRows[1];
    expect(within(childRow).getByText('COMP-A')).toBeTruthy(); // repeated component number
    expect(within(childRow).getByText('200')).toBeTruthy(); // own PO
    expect(within(childRow).getByText('4')).toBeTruthy(); // own line

    // Collapse again.
    fireEvent.click(within(table).getAllByRole('row').slice(1)[0].querySelector('.component-orders__expand')!);
    expect(within(table).getAllByRole('row').slice(1)).toHaveLength(2);
  });

  it('applies due-date presentation states: missing yellow, on/before Friday cutoff red, after normal', async () => {
    fetchMock.mockResolvedValue(response([
      { componentPart: 'MISSING', displayLine: line({ componentPart: 'MISSING', dueDate: null }), additionalLines: [] },
      { componentPart: 'LATE', displayLine: line({ componentPart: 'LATE', dueDate: '2026-01-05' }), additionalLines: [] },
      { componentPart: 'NORMAL', displayLine: line({ componentPart: 'NORMAL', dueDate: '2999-01-01' }), additionalLines: [] },
    ]));

    render(<ComponentOrdersPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const table = await screen.findByRole('table');
    const rows = within(table).getAllByRole('row').slice(1);

    // PO Due is the 6th column (index 5): missing → yellow exception class, late → red, normal → neither.
    const dueCell = (row: HTMLElement) => row.querySelectorAll('td')[5];
    expect(dueCell(rows[0]).className).toContain('component-orders__due--missing');
    expect(dueCell(rows[1]).className).toContain('component-orders__due--late');
    expect(dueCell(rows[2]).className).not.toContain('--late');
  });

  it('renders field display rules: Confirmed Yes/No, KSS or blank, Weeks LT "-" for null/zero and ceil otherwise, Missing master data, raw open quantity', async () => {
    fetchMock.mockResolvedValue(response([
      {
        componentPart: 'RULES',
        displayLine: line({
          description: null, // missing part-master row
          leadTimeDays: 0, // zero resolved days → "-"
          confirmed: false,
          isKss: true,
          openQuantity: 12.5, // raw value — no UOM conversion
        }),
        additionalLines: [line({ componentPart: 'RULES', poNumber: '999', leadTimeDays: 8 })], // ceil(8/7) = 2
      },
    ]));

    render(<ComponentOrdersPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const table = await screen.findByRole('table');

    expect(screen.getByText('Missing master data')).toBeTruthy();
    // Confirmed: No on the collapsed row.
    const rows = within(table).getAllByRole('row').slice(1);
    expect(within(rows[0]).getByText('No')).toBeTruthy();
    expect(within(rows[0]).getByText('KSS')).toBeTruthy();
    // Weeks LT: "-" for zero days on the collapsed row (3rd column, index 2).
    expect(rows[0].querySelectorAll('td')[2].textContent).toBe('-');
    // Raw open quantity, unconverted.
    expect(within(rows[0]).getByText(/12\.5/)).toBeTruthy();

    // Expand and check the child row's ceil weeks LT (8 days → 2) and blank KSS.
    fireEvent.click(within(rows[0]).getByRole('button', { name: /Expand RULES/ }));
    const expandedRows = within(table).getAllByRole('row').slice(1);
    expect(expandedRows[1].querySelectorAll('td')[2].textContent).toBe('2');
  });
});

describe('ComponentOrdersPanel delivered filters', () => {
  it('filters by component number (case-insensitive partial match) and KSS only, independently and combined', async () => {
    fetchMock.mockResolvedValue(response([
      { componentPart: 'KSS-PART', displayLine: line({ componentPart: 'KSS-PART', isKss: true }), additionalLines: [] },
      { componentPart: 'plain-part', displayLine: line({ componentPart: 'plain-part' }), additionalLines: [] },
    ]));

    render(<ComponentOrdersPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const table = await screen.findByRole('table');

    // Component number filter: partial, case-insensitive.
    fireEvent.change(screen.getByPlaceholderText(/Filter by part number/i), { target: { value: 'KSS' } });
    expect(within(table).getAllByRole('row').slice(1)).toHaveLength(1);
    expect(screen.getByText('KSS-PART')).toBeTruthy();

    // Clear, then KSS-only filter.
    fireEvent.change(screen.getByPlaceholderText(/Filter by part number/i), { target: { value: '' } });
    fireEvent.click(screen.getByRole('checkbox', { name: 'KSS only' }));
    expect(within(table).getAllByRole('row').slice(1)).toHaveLength(1);
    expect(screen.getByText('KSS-PART')).toBeTruthy();

    // Combined: KSS-only + a component number that matches only the non-KSS part → empty.
    fireEvent.change(screen.getByPlaceholderText(/Filter by part number/i), { target: { value: 'plain' } });
    expect(await screen.findByText(/No components have qualifying open PO lines/)).toBeTruthy();
  });

  it('filters collapsed groups by Credit Hold and CIA using their displayed line while expanded children retain their own facts', async () => {
    fetchMock.mockResolvedValue(response([
      {
        componentPart: 'CREDIT',
        displayLine: line({ componentPart: 'CREDIT', isCreditHold: true }),
        additionalLines: [line({ componentPart: 'CREDIT', poNumber: '101', isCreditHold: false, isCia: true })],
      },
      { componentPart: 'CIA', displayLine: line({ componentPart: 'CIA', isCia: true }), additionalLines: [] },
    ]));
    render(<ComponentOrdersPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const table = await screen.findByRole('table');
    const checkboxes = screen.getAllByRole('checkbox');

    fireEvent.click(checkboxes[1]);
    expect(within(table).getAllByRole('row').slice(1)).toHaveLength(1);
    fireEvent.click(screen.getByRole('button', { name: /Expand CREDIT/ }));
    const rows = within(table).getAllByRole('row').slice(1);
    expect(rows).toHaveLength(2);
    expect(rows[0].querySelectorAll('td')[13].textContent).toBe('✓');
    expect(rows[1].querySelectorAll('td')[14].textContent).toBe('✓');

    fireEvent.click(checkboxes[1]);
    fireEvent.click(checkboxes[2]);
    expect(within(table).getAllByRole('row').slice(1)).toHaveLength(1);
    expect(within(table).getAllByRole('row').slice(1)[0].querySelectorAll('td')[0].textContent).toBe('CIA');
  });

  it('shows a two-line clickable comment preview and preserves full multiline text in a closable dialog', async () => {
    const user = userEvent.setup();
    const comments = 'Line one\nLine two\nLine three';
    fetchMock.mockResolvedValue(response([
      { componentPart: 'COMMENTS', displayLine: line({ componentPart: 'COMMENTS', currentComments: comments }), additionalLines: [line({ componentPart: 'COMMENTS', poNumber: '101', currentComments: comments })] },
    ]));
    render(<ComponentOrdersPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const preview = await screen.findByRole('button', { name: 'View comments for COMMENTS' });
    expect(preview.textContent).toBe(comments);
    fireEvent.click(preview);
    const dialog = screen.getByRole('dialog');
    expect(within(dialog).getByText((_, element) => element?.tagName === 'PRE' && element.textContent === comments)).toBeTruthy();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).toBeNull();

    preview.focus();
    await user.keyboard('{Enter}');
    expect(screen.getByRole('dialog')).toBeTruthy();
    fireEvent.click(screen.getByRole('button', { name: 'Close Current Comments' }));
    expect(screen.queryByRole('dialog')).toBeNull();
    fireEvent.click(screen.getByRole('button', { name: /Expand COMMENTS/ }));
    expect(within(screen.getByRole('table')).getAllByRole('button', { name: 'View comments for COMMENTS' })).toHaveLength(1);
  });

  it('keeps QAD rows visible while unavailable enrichment displays a notice, em dashes, and disabled filters', async () => {
    fetchMock.mockResolvedValue(response([
      { componentPart: 'QAD', displayLine: line({ componentPart: 'QAD', isCreditHold: true, isCia: true, currentComments: 'Old comment' }), additionalLines: [] },
    ], 'Unavailable'));
    render(<ComponentOrdersPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const table = await screen.findByRole('table');
    const row = within(table).getAllByRole('row')[1];

    expect(screen.getByText('Shortages enrichment is unavailable. Credit Hold, CIA, and Current Comments may not be current.')).toBeTruthy();
    expect(within(row).getByText('QAD')).toBeTruthy();
    expect(row.querySelectorAll('td')[13].textContent).toBe('—');
    expect(row.querySelectorAll('td')[14].textContent).toBe('—');
    expect(row.querySelectorAll('td')[15].textContent).toBe('—');
    expect(screen.getAllByRole('checkbox')[1]).toBeDisabled();
    expect(screen.getAllByRole('checkbox')[2]).toBeDisabled();
  });
});

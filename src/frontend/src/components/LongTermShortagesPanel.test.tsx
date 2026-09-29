import { beforeEach, describe, expect, it, vi } from 'vitest';
import { readFileSync } from 'node:fs';
import { mkdtempSync, rmSync, writeFileSync, existsSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import type { LongTermShortageRow, LongTermShortagesScreen as LongTermShortagesResponse } from '../api/longTermShortagesApi';
import { compactFixture, projectionFixture } from '../longTermShortages/compactScreenFixture.test-support';
import { exportLongTermShortages, fetchLongTermShortages } from '../api/longTermShortagesApi';
import { saveLongTermShortagesWorkbook } from '../longTermShortages/saveLongTermShortagesWorkbook';
import { LongTermShortagesPanel } from './LongTermShortagesPanel';
import { fetchComponentDetail } from '../api/componentDetailApi';
import { fetchApprovedVendors } from '../api/approvedVendorsApi';
import { fetchLongTermShortagePurchasing } from '../api/longTermShortagesApi';

const fetchMock = vi.fn();
const exportMock = vi.fn();
const saveWorkbookMock = vi.fn();
const purchasingMock = vi.fn();
const projectionMock = vi.fn();
const EDGE_PATH = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
vi.mock('../api/longTermShortagesApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/longTermShortagesApi')>()),
  fetchLongTermShortages: (...args: Parameters<typeof fetchLongTermShortages>) => fetchMock(...args),
  exportLongTermShortages: (...args: Parameters<typeof exportLongTermShortages>) => exportMock(...args),
  fetchLongTermShortagePurchasing: (...args: Parameters<typeof fetchLongTermShortagePurchasing>) => purchasingMock(...args),
  fetchLongTermShortageProjectionDetail: (...args: unknown[]) => projectionMock(...args),
}));
vi.mock('../longTermShortages/saveLongTermShortagesWorkbook', () => ({
  saveLongTermShortagesWorkbook: (...args: Parameters<typeof saveLongTermShortagesWorkbook>) => saveWorkbookMock(...args),
}));
vi.mock('../api/componentDetailApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/componentDetailApi')>()),
  fetchComponentDetail: vi.fn(),
}));
vi.mock('../api/approvedVendorsApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/approvedVendorsApi')>()),
  fetchApprovedVendors: vi.fn(),
}));

function bucket(weekNumber: number | null, projectedQoh: number | string, severity = 'Clear') {
  return { weekNumber, weekStart: weekNumber === null ? null : '2026-10-04', grossRequirements: '98.2456140337', scheduledReceipts: 0, plannedOrdersDue: 0, plannedOrdersRelease: weekNumber === 1 ? '49.125' : 0, projectedQoh, severity, unconfirmedReceipts: 0, confirmedEnding: projectedQoh, allReceiptsEnding: projectedQoh, planningEnding: projectedQoh, allReceiptsPlanningEnding: projectedQoh, lowestProjectedBalance: projectedQoh, lowestConfirmedBalance: projectedQoh, lowestAllReceiptsBalance: projectedQoh, overdueReceipts: 0, includesUnconfirmed: false };
}

function row(overrides: Partial<LongTermShortageRow> = {}): LongTermShortageRow {
  return {
    componentPart: 'CRITICAL', unitOfMeasure: 'EA', qadStatus: 'P', description: 'Bolt', planner: 'Ann', buyerPlannerCode: 'BUY', openingQoh: '12.125', safetyStockState: 'Resolved', safetyStock: 5,
    severity: 'CriticalShort', firstShortDate: '2026-10-05', demandParentParts: ['PARENT-1'], past: bucket(null, '12.125'),
    weeks: Array.from({ length: 26 }, (_, index) => bucket(index + 1, index === 0 ? '-2.4' : '12.125', index === 0 ? 'CriticalShort' : 'Clear')),
    evidence: [{ evidenceOrdinal: 1, type: 'SUPPLYP', dueDate: '2026-10-05', releaseDate: '2026-09-28', quantity: '49.125', category: 'PlannedOrdersRelease', sourceNumber: null, sourceLine: null, sourceLine2: null, sourceRowId: '0001', isPoReceipt: false, poConfirmed: null }], episodes: [], dataQualityWarning: null,
    presentation: { manufacturerItem: 'MFG-1', poNumber: 'PO-1', poLine: 2, poDueDate: '2026-10-06', poOpenQuantity: '49.125', poConfirmed: true, isKss: true }, firstAtRiskDate: null, effectivePmCode: 'P', partStatusDescription: 'PROTO', orderPeriodDays: 7, safetyTimeWorkingDays: 0, manufacturingLeadWorkingDays: null, purchasingLeadCalendarDays: 14, cumulativeLeadCalendarDays: 21, sitePlanningPresent: true, ...overrides,
  };
}

function response(rows: LongTermShortageRow[], isStale = false, allRows = rows): LongTermShortagesResponse {
  return { snapshotId: 'snap-1', refreshDate: '2026-10-05', isStale, warning: isStale ? 'QAD unavailable; retained result shown.' : null,
    components: rows.map((r, index) => ({ ...compactFixture(r), all: compactFixture(allRows[index]).all })),
    weeks: rows[0]?.weeks.map(w => ({ weekNumber: w.weekNumber!, weekStart: w.weekStart!, labelDate: '2026-10-05' })) ?? [],
    acquiredAtUtc: '2026-10-05T12:00:00Z', consistencyMode: 'PRO2_READ_UNCOMMITTED' };
}

function purchasing(lines: Array<{ poNumber: string; poLine: number; dueDate: string | null; openQuantity: number; confirmed: boolean | null; supplierDisplay: string | null }> = [], currentComment: string | null = null) {
  return { commentAvailable: true, currentComment, openPurchaseOrders: lines.map((line) => ({ componentPart: 'CRITICAL', description: null, leadTimeDays: null, buyerDisplay: null, manufacturerItem: null, isKss: false, trackingInfo: null, isCreditHold: null, isCia: null, currentComments: null, ...line })) };
}

beforeEach(() => {
  fetchMock.mockReset(); exportMock.mockReset(); saveWorkbookMock.mockReset(); purchasingMock.mockReset().mockResolvedValue(purchasing());
  projectionMock.mockReset().mockImplementation((_workspace, snapshotId, componentPart) => Promise.resolve({ ...projectionFixture(row()), snapshotId, componentPart }));
  vi.mocked(fetchComponentDetail).mockReset().mockResolvedValue({
    site: 'SW', componentPart: 'CRITICAL', description: 'Bolt', partStatusCode: 'P', partStatusDescription: 'PROTO',
    iosCode: null, netQuantityOnHand: 12, nonNetQuantityOnHand: 0, standardCost: null, qctc: null,
    timeFence: null, safetyTime: null, safetyStock: 5, buyerPlanner: 'BUY', purchaseLeadTimeDays: 14,
    inspectionLeadTimeDays: null, cumulativeLeadTimeDays: 21, minimumOrderQuantity: null, orderMultiple: null,
    loadedAtUtc: '2026-10-05T12:00:00Z', isStale: false, warning: null,
  });
  vi.mocked(fetchApprovedVendors).mockReset().mockResolvedValue([]);
});

describe('LongTermShortagesPanel', () => {
  it('hydrates only the selected snapshot detail and keeps display actions local', async () => {
    fetchMock.mockResolvedValue(response([row()]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table');
    expect(projectionMock).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'CRITICAL' }));
    expect(await screen.findByText('PARENT-1')).toBeInTheDocument();
    expect(projectionMock).toHaveBeenCalledWith('ws-1', 'snap-1', 'CRITICAL', expect.objectContaining({ horizonWeeks: 26 }));
    fireEvent.keyDown(document, { key: 'Escape' });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Include Unconfirmed Receipts' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show All' }));
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(projectionMock).toHaveBeenCalledTimes(1);
  });

  it('rejects mismatched detail without showing newer data under the screen snapshot', async () => {
    fetchMock.mockResolvedValue(response([row()]));
    projectionMock.mockResolvedValue({ ...projectionFixture(row()), snapshotId: 'snap-2', demandParentParts: ['NEWER-PARENT'] });
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table');
    fireEvent.click(screen.getByRole('button', { name: 'CRITICAL' }));
    expect(await screen.findByText(/Snapshot projection detail unavailable/)).toBeInTheDocument();
    expect(screen.queryByText('NEWER-PARENT')).not.toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('does not fetch before MPS is loaded', () => {
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId={null} />);
    expect(screen.getByText(/Load the MPS dashboard/)).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
    expect(purchasingMock).not.toHaveBeenCalled();
  });

  it('renders a 26 Sunday-week grid, defaults to shortages, filters locally, and exposes stale status', async () => {
    fetchMock.mockResolvedValue(response([row(), row({ componentPart: 'CLEAR', qadStatus: null, planner: 'Bob', severity: 'Healthy', firstShortDate: null, weeks: [bucket(1, '12.125')] })], true));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const metadata = await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    expect(purchasingMock).not.toHaveBeenCalled();
    const weeks = metadata;
    expect(within(weeks).getByRole('columnheader', { name: /Week 26/i })).toBeInTheDocument();
    expect(screen.queryByText(/QADPro2 near-real-time Pro2 reporting replica/)).not.toBeInTheDocument();
    expect(within(metadata).queryByText('CLEAR')).not.toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent(/retained result/);
    const firstWeekCell = weeks.querySelector('tbody td:nth-child(8)');
    expect(firstWeekCell).toHaveTextContent('-2');
    expect(firstWeekCell?.className).toContain('long-term-shortages__balance--negative');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show All' }));
    await screen.findByText('CLEAR');
    fireEvent.change(screen.getByLabelText('Buyer / Planner'), { target: { value: 'Bob' } });
    await waitFor(() => expect(screen.getByRole('table', { name: 'Workspace Shortages weekly balances' })).toHaveTextContent('CLEAR'));
    fireEvent.click(screen.getByRole('checkbox', { name: 'KSS' }));
    expect(screen.getByRole('table', { name: 'Workspace Shortages weekly balances' })).toHaveTextContent('CLEAR');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Status' }));
    fireEvent.change(screen.getByLabelText('Sort'), { target: { value: 'component' } });
    fireEvent.change(screen.getByLabelText('Comp'), { target: { value: 'clear' } });
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('shows the specific source failure without repeating unavailable', async () => {
    fetchMock.mockRejectedValue(new Error('QADPro2 returned incomplete component results. Retry the report.'));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    expect(await screen.findByRole('alert')).toHaveTextContent('QADPro2 returned incomplete component results. Retry the report.');
    expect(screen.queryByText(/Workspace Shortages is unavailable: Workspace Shortages Unavailable/i)).not.toBeInTheDocument();
  });

  it('switches receipt mode locally and refreshes manufactured population while retaining the grid', async () => {
    let resolveManufactured!: (result: LongTermShortagesResponse) => void;
    fetchMock.mockResolvedValueOnce(response([row()], false, [row({ componentPart: 'CRITICAL', severity: 'Healthy' })]))
      .mockImplementationOnce(() => new Promise<LongTermShortagesResponse>((resolve) => { resolveManufactured = resolve; }));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Include Unconfirmed Receipts' }));
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole('table', { name: 'Workspace Shortages weekly balances' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show All' }));
    expect(screen.getByRole('table', { name: 'Workspace Shortages weekly balances' })).toHaveTextContent('Healthy');
    fireEvent.click(screen.getByRole('checkbox', { name: 'Include Manufactured Parts' }));
    await waitFor(() => expect(fetchMock).toHaveBeenLastCalledWith('ws-1', 'snap-1', { includeManufacturedParts: true, includePhantoms: false, includeUnconfirmed: false, horizonWeeks: 26, showAll: true }));
    expect(screen.getByRole('status')).toHaveTextContent('Refreshing…');
    expect(screen.getByRole('table', { name: 'Workspace Shortages weekly balances' })).toBeInTheDocument();
    resolveManufactured(response([row()]));
    await waitFor(() => expect(screen.queryByText('Refreshing…')).not.toBeInTheDocument());
    fireEvent.click(screen.getByRole('checkbox', { name: 'Include Manufactured Parts' }));
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it('reuses a loaded horizon and avoids duplicate in-flight refreshes', async () => {
    let resolveLonger!: (result: LongTermShortagesResponse) => void;
    fetchMock.mockResolvedValueOnce(response([row()]))
      .mockImplementationOnce(() => new Promise<LongTermShortagesResponse>((resolve) => { resolveLonger = resolve; }));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    fireEvent.change(screen.getByLabelText('Horizon'), { target: { value: '52' } });
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
    fireEvent.change(screen.getByLabelText('Horizon'), { target: { value: '26' } });
    fireEvent.change(screen.getByLabelText('Horizon'), { target: { value: '52' } });
    expect(fetchMock).toHaveBeenCalledTimes(2);
    resolveLonger(response([row({ weeks: Array.from({ length: 52 }, (_, index) => bucket(index + 1, '12.125')) })]));
    await waitFor(() => expect(screen.queryByText('Refreshing…')).not.toBeInTheDocument());
  });

  it('contains the grid in a keyboard-scrollable panel with one aligned matrix and a shared modal', async () => {
    fetchMock.mockResolvedValue(response([row()]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const grid = await screen.findByTestId('workspace-shortages-grid');
    expect(grid).toHaveAttribute('tabindex', '0');
    expect(within(grid).getByRole('table', { name: 'Workspace Shortages weekly balances' })).toBeInTheDocument();
    expect(within(grid).getAllByRole('table')).toHaveLength(1);
    expect(grid.closest('.long-term-shortages')).toHaveAttribute('aria-labelledby', 'long-term-shortages-heading');
    expect(within(grid).getByRole('columnheader', { name: 'Buyer / Planner' })).toBeInTheDocument();
  });

  it('keeps severity labels compact while exposing full warnings on hover, focus, and in component information', async () => {
    const warning = 'Selected-site planning data missing; safety stock and lead times unknown.';
    fetchMock.mockResolvedValue(response([
      row({ componentPart: 'UNKNOWN', severity: 'SafetyStockUnavailable', safetyStock: null, safetyStockState: 'SelectedSiteValueMissing', dataQualityWarning: warning, weeks: [bucket(1, 12)] }),
      row({ componentPart: 'FUTURE', severity: 'FutureShort', dataQualityWarning: null }),
      row({ componentPart: 'SAFETY', severity: 'SafetyStockShort', dataQualityWarning: null, weeks: [bucket(1, 2, 'SafetyStockShort')] }),
      row(),
    ]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const metadata = await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Show All' }));
    const unknown = within(metadata).getByRole('row', { name: 'UNKNOWN: SafetyStockUnavailable' });
    const severity = within(unknown).getByLabelText(`SS Unknown: ${warning}`);
    expect(severity).toHaveTextContent('SS Unknown');
    expect(severity).toHaveAttribute('tabindex', '0');
    expect(unknown.querySelector('td:nth-child(7)')).toHaveTextContent('SS Unknown');
    expect(within(metadata).queryByText(warning)).not.toBeInTheDocument();
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
    fireEvent.mouseOver(severity);
    const tooltip = screen.getByRole('tooltip');
    expect(tooltip).toHaveTextContent(warning);
    expect(severity).toHaveAttribute('aria-describedby', tooltip.id);
    expect(tooltip).toHaveClass('long-term-shortages__severity-tooltip');
    expect(metadata.contains(tooltip)).toBe(false);
    fireEvent.mouseOut(severity);
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
    severity.blur();
    severity.focus();
    expect(severity).toHaveFocus();
    expect(await screen.findByRole('tooltip')).toHaveTextContent(warning);
    expect(within(metadata).getByRole('row', { name: 'FUTURE: FutureShort' })).toHaveTextContent('FutureShort');
    expect(within(metadata).getByRole('row', { name: 'SAFETY: SafetyStockShort' }).querySelector('.long-term-shortages__comp')).toHaveClass('long-term-shortages__comp--safety');
    expect(within(metadata).getByRole('row', { name: 'CRITICAL: CriticalShort' }).querySelector('.long-term-shortages__comp')).toHaveClass('long-term-shortages__comp--critical');
    fireEvent.click(within(unknown).getByRole('button', { name: 'UNKNOWN' }));
    expect(within(screen.getByRole('dialog', { name: 'Component Information' })).getByText(warning)).toBeInTheDocument();
  });

  it('uses compact aligned On Hand and wrapped QAD Status headers without changing values', async () => {
    fetchMock.mockResolvedValue(response([row()]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    const metadata = await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    const onHand = within(metadata).getByRole('columnheader', { name: 'On Hand' });
    const qad = within(metadata).getByRole('columnheader', { name: 'QAD Status' });
    expect(onHand).toHaveClass('long-term-shortages__num');
    expect(qad.querySelector('br')).toBeInTheDocument();
    expect(qad.querySelector('[aria-hidden="true"]')).toHaveTextContent('QADStatus');
    const cells = within(metadata).getByRole('row', { name: 'CRITICAL: CriticalShort' }).querySelectorAll('td');
    expect(cells[1]).toHaveTextContent('P');
    expect(cells[5]).toHaveClass('long-term-shortages__num');
    expect(cells[5]).toHaveTextContent('12');
    expect(screen.getByTestId('workspace-shortages-grid')).toHaveAttribute('tabindex', '0');
  });

  it('constrains informational widths while retaining the bounded horizontal projection grid', () => {
    // jsdom does not lay out table columns; verify the CSS rules used by the rendered classes.
    const css = readFileSync(resolve(process.cwd(), 'src/components/LongTermShortagesPanel.css'), 'utf8');
    expect(css).toMatch(/\.long-term-shortages__grid\s*{[^}]*overflow:\s*auto/);
    expect(css).toMatch(/\.long-term-shortages\s*{[^}]*overflow:\s*hidden/);
    expect(css).toMatch(/--comp-width:\s*104px/);
    expect(css).toMatch(/--description-width:\s*180px/);
    expect(css).toMatch(/--qad-status-width:\s*44px/);
    expect(css).toMatch(/--on-hand-width:\s*88px/);
    expect(css).toMatch(/--severity-width:\s*130px/);
    expect(css).toMatch(/\.long-term-shortages__severity-tooltip\s*{[^}]*position:\s*fixed/);
  });

  it.skipIf(!existsSync(EDGE_PATH))('keeps the seven corner headers aligned and above weekly cells through scroll, resize, and scaling in Edge', async () => {
    fetchMock.mockResolvedValue(response(Array.from({ length: 35 }, (_, index) => row({ componentPart: `PART-${index}`, description: 'Bolt', planner: 'Ann' }))));
    const { container } = render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    const matrix = container.querySelector('.long-term-shortages__matrix')!;
    const css = readFileSync(resolve(process.cwd(), 'src/components/LongTermShortagesPanel.css'), 'utf8');
    const theme = readFileSync(resolve(process.cwd(), 'src/index.css'), 'utf8');
    const temp = mkdtempSync(resolve(tmpdir(), 'kst-frozen-grid-'));
    try {
      const fixture = `<!doctype html><meta charset="utf-8"><style>${theme}\n${css}\nbody { margin: 0; } .long-term-shortages { width: 1050px; height: 430px; box-sizing: border-box; }</style><section class="long-term-shortages"><div class="long-term-shortages__grid" style="height: 100%; flex: none">${matrix.outerHTML}</div></section><output id="layout-result"></output><script>
        const grid = document.querySelector('.long-term-shortages__grid');
        const table = grid.querySelector('table');
        const headers = [...table.tHead.rows[0].cells];
        const cells = [...table.tBodies[0].rows[0].cells];
        const failures = [];
        const close = (a, b) => Math.abs(a - b) < 1;
        function verify(width, scale) {
          document.querySelector('.long-term-shortages').style.width = width + 'px';
          document.body.style.zoom = String(scale);
          grid.scrollLeft = 380;
          grid.scrollTop = 170;
          const edge = grid.getBoundingClientRect().left + grid.clientLeft;
          const top = grid.getBoundingClientRect().top + grid.clientTop;
          for (let i = 0; i < 7; i++) {
            const h = headers[i].getBoundingClientRect();
            const c = cells[i].getBoundingClientRect();
            if (!close(h.left, c.left) || !close(h.width, c.width) || !close(h.top, top)) failures.push('frozen header ' + i + ' position');
            if (getComputedStyle(headers[i]).backgroundColor === 'rgba(0, 0, 0, 0)' || getComputedStyle(cells[i]).backgroundColor === 'rgba(0, 0, 0, 0)') failures.push('transparent frozen cell ' + i);
          }
          const boundary = headers[6].getBoundingClientRect().right;
          const week = headers[7].getBoundingClientRect();
          const weeklyValue = cells[7].getBoundingClientRect();
          const probe = Math.max(edge + 2, Math.min(boundary - 2, week.right - 2));
          if (week.left >= boundary || weeklyValue.left >= boundary) failures.push('weekly cells did not scroll beneath frozen region');
          if (document.elementFromPoint(probe, headers[0].getBoundingClientRect().top + 10) !== headers.find(h => { const r = h.getBoundingClientRect(); return r.left <= probe && r.right > probe; })) failures.push('weekly header overlaps frozen header');
          const visibleCells = [...table.tBodies[0].rows[8].cells];
          if (document.elementFromPoint(probe, visibleCells[0].getBoundingClientRect().top + 10) !== visibleCells.find(c => { const r = c.getBoundingClientRect(); return r.left <= probe && r.right > probe; })) failures.push('weekly value overlaps frozen cell');
          if (!close(headers[20].getBoundingClientRect().top, top)) failures.push('weekly header lost vertical sticky position');
          if (grid.scrollLeft < 100 || grid.scrollTop < 100) failures.push('bounded two-axis scrolling unavailable');
          if (getComputedStyle(grid).overflowX !== 'auto' || getComputedStyle(grid).overflowY !== 'auto') failures.push('scrollbar missing');
        }
        for (const [width, scale] of [[1050, 1], [900, 1], [1050, 1.25], [900, 1.5]]) verify(width, scale);
        document.getElementById('layout-result').textContent = JSON.stringify(failures);
      </script>`;
      const file = resolve(temp, 'grid.html');
      writeFileSync(file, fixture);
      const result = spawnSync(EDGE_PATH, ['--headless=new', '--disable-gpu', '--no-first-run', `--user-data-dir=${resolve(temp, 'profile')}`, '--dump-dom', `file:///${file.replace(/\\/g, '/')}`], { encoding: 'utf8', timeout: 30000, windowsHide: true });
      expect(result.error).toBeUndefined();
      expect(result.status).toBe(0);
      const match = result.stdout.match(/<output id="layout-result">([^<]*)<\/output>/);
      expect(match, result.stderr).not.toBeNull();
      expect(JSON.parse(match![1])).toEqual([]);
    } finally {
      rmSync(temp, { recursive: true, force: true });
    }
  }, 45000);

  it('opens selected-component purchasing detail without reloading the shortage matrix, then exports only filtered rows', async () => {
    exportMock.mockResolvedValue({ blob: new Blob(['workbook']), fileName: 'Workspace-Shortages-2026-10-05.xlsx' });
    saveWorkbookMock.mockResolvedValue({ kind: 'saved', fileName: 'Workspace-Shortages-2026-10-05.xlsx' });
    fetchMock.mockResolvedValue(response([row(), row({ componentPart: 'OTHER', planner: 'Bob' })]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    fireEvent.change(screen.getByLabelText('Comp'), { target: { value: 'critical' } });
    fireEvent.click(screen.getByRole('button', { name: 'CRITICAL' }));
    const dialog = screen.getByRole('dialog', { name: 'Component Information' });
    expect(within(dialog).getByRole('heading', { name: 'Past' })).toBeInTheDocument();
    expect(within(dialog).getByRole('heading', { name: 'Shortage Snapshot' })).toBeInTheDocument();
    expect(within(dialog).getByRole('heading', { name: 'Open Purchase Orders' })).toBeInTheDocument();
    expect(within(dialog).queryByText('PlannedOrdersRelease')).not.toBeInTheDocument();
    expect(within(dialog).queryByRole('heading', { name: 'Raw MRP Evidence' })).not.toBeInTheDocument();
    expect(within(dialog).getByRole('complementary', { name: 'Workspace Shortages details' })).toBeInTheDocument();
    await waitFor(() => expect(vi.mocked(fetchComponentDetail)).toHaveBeenCalledWith('ws-1', 'CRITICAL'));
    await waitFor(() => expect(purchasingMock).toHaveBeenCalledWith('ws-1', 'snap-1', 'CRITICAL', expect.objectContaining({ horizonWeeks: 26 })));
    expect(purchasingMock).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(await within(dialog).findByText('Net QOH')).toBeInTheDocument();
    expect(within(dialog).getByText('Inventory / Lot Locations')).toBeInTheDocument();
    expect(within(dialog).queryByText(/KSS is visibility only/)).not.toBeInTheDocument();
    expect(within(dialog).queryByText(/Lead times annotate urgency/)).not.toBeInTheDocument();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Export displayed rows' }));
    await waitFor(() => expect(exportMock).toHaveBeenCalledWith('ws-1', 'snap-1', ['CRITICAL'], { includeManufacturedParts: false, includePhantoms: false, includeUnconfirmed: false, horizonWeeks: 26, showAll: false }));
  });

  it('keeps the selected shortage snapshot visible when the PO detail request fails', async () => {
    purchasingMock.mockRejectedValue(new Error('PO source unavailable'));
    fetchMock.mockResolvedValue(response([row()]));
    render(<LongTermShortagesPanel assignmentId="ws-1" snapshotId="snap-1" />);
    await screen.findByRole('table', { name: 'Workspace Shortages weekly balances' });
    fireEvent.click(screen.getByRole('button', { name: 'CRITICAL' }));
    const dialog = screen.getByRole('dialog', { name: 'Component Information' });
    expect(within(dialog).getByRole('heading', { name: 'Shortage Snapshot' })).toBeInTheDocument();
    expect(await within(dialog).findByText(/Open purchase orders unavailable/)).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });
});

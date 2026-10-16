import { describe, expect, it } from 'vitest';
import type { MpsDashboardResponseDto } from '../api/client';
import { DEFAULT_FISCAL_CALENDAR_SETTINGS } from '../fiscal/fiscalCalendar';
import { mpsExportRequest } from './mpsExport';

function dashboard(labels: string[]): MpsDashboardResponseDto {
  return { snapshot: { snapshotId: 'snapshot', workspaceId: 'workspace', site: 'SW', createdAtUtc: '2026-10-12T12:00:00Z', lastSuccessfulRefreshAtUtc: '2026-10-12T12:00:00Z', status: 'current', resolvedParentPartCount: 1, sourceRowCount: 1, isRefreshInProgress: false, lastRefreshError: null }, dateBasis: 'dueDate', horizonWeeks: 2,
    parts: [{ parentPart: 'PARENT', description: 'Part', buckets: [{ kind: 'falldown', weekLabel: null, quantity: 0, executionStatus: 'none', containsPlannedWork: false, containsExplicitlyScheduledWork: false }, ...labels.map(weekLabel => ({ kind: 'weekly' as const, weekLabel, quantity: 1, executionStatus: 'none' as const, containsPlannedWork: false, containsExplicitlyScheduledWork: false }))] }] };
}
describe('MPS export dates — independently specified Sunday/Saturday and fiscal expectations', () => {
  it('uses the loaded Monday labels on Sunday without moving them to the preceding week', () => {
    const result = mpsExportRequest(dashboard(['2026-10-19', '2026-10-26']), 'workspace', 'dueDate', 2, DEFAULT_FISCAL_CALENDAR_SETTINGS, false, new Date(2026, 9, 18, 12));
    expect(result.businessDate).toBe('2026-10-18'); expect(result.parent).toBeNull();
    expect(result.fiscalLabels).toEqual(['FY2027 Q2 P4', 'FY2027 Q2 P5']);
    expect(result.metadata).toEqual([{ label: 'Calendar', value: JSON.stringify(DEFAULT_FISCAL_CALENDAR_SETTINGS) }, { label: 'Export date', value: '10/18/2026' }]);
  });
  it('accepts Saturday labels but requires a reload when that same loaded view crosses Sunday', () => {
    const loaded = dashboard(['2026-10-12', '2026-10-19']);
    expect(mpsExportRequest(loaded, 'workspace', 'dueDate', 2, DEFAULT_FISCAL_CALENDAR_SETTINGS, false, new Date(2026, 9, 17, 23, 59)).fiscalLabels).toEqual(['FY2027 Q2 P4', 'FY2027 Q2 P4']);
    expect(() => mpsExportRequest(loaded, 'workspace', 'dueDate', 2, DEFAULT_FISCAL_CALENDAR_SETTINGS, false, new Date(2026, 9, 18))).toThrow(/business week changed/);
  });
  it('uses the same loaded labels on Monday and applies changed fiscal settings without changing the dates', () => {
    const loaded = dashboard(['2026-10-19', '2026-10-26']);
    const shifted = { anchorFiscalYear: 2027, anchorStartDate: '2026-07-05', exceptions: [] };
    expect(mpsExportRequest(loaded, 'workspace', 'dueDate', 2, shifted, false, new Date(2026, 9, 19)).fiscalLabels).toEqual(['FY2027 Q2 P4', 'FY2027 Q2 P4']);
    expect(loaded.parts[0].buckets.slice(1).map(b => b.weekLabel)).toEqual(['2026-10-19', '2026-10-26']);
  });
  it('retains stale-source status within the correct loaded week and rejects missing or inconsistent labels', () => {
    const loaded = dashboard(['2026-10-19', '2026-10-26']);
    expect(mpsExportRequest(loaded, 'workspace', 'dueDate', 2, DEFAULT_FISCAL_CALENDAR_SETTINGS, true, new Date(2026, 9, 18)).isStale).toBe(true);
    for (const labels of [['2026-10-19'], ['2026-10-19', '2026-11-02']]) expect(() => mpsExportRequest(dashboard(labels), 'workspace', 'dueDate', 2, DEFAULT_FISCAL_CALENDAR_SETTINGS, false, new Date(2026, 9, 18))).toThrow(/loaded week labels/);
    expect(() => mpsExportRequest(loaded, 'workspace', 'releaseDate', 2, DEFAULT_FISCAL_CALENDAR_SETTINGS, false, new Date(2026, 9, 18))).toThrow(/selections/);
  });
});

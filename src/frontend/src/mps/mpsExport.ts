import type { MpsDashboardResponseDto } from '../api/client';
import { analyticalRequest } from '../api/analyticalExportApi';
import type { MpsDateBasis } from '../api/mpsApi';
import type { FiscalCalendarSettings } from '../fiscal/types';
import { getFiscalDisplayInfo, startOfBusinessWeek } from '../fiscal/fiscalCalendar';
import { formatLocalDate } from '../planningWorkbook/planningWorkbookSummary';
import { parseIsoDateOnly } from './mpsPresentation';
import { localIsoDate } from '../shared/localDate';

export function mpsExportRequest(dashboard: MpsDashboardResponseDto, assignmentId: string, basis: MpsDateBasis, horizon: number, fiscal: FiscalCalendarSettings, stale: boolean, now = new Date()) {
  if (!dashboard.snapshot.snapshotId || !dashboard.snapshot.site || dashboard.dateBasis !== basis || dashboard.horizonWeeks !== horizon) throw new Error('MPS selections are loading or incompatible. Reload the current view before exporting.');
  const buckets = dashboard.parts[0]?.buckets.slice(1) ?? [];
  const sunday = startOfBusinessWeek(now);
  const labels = Array.from({ length: horizon }, (_, index) => {
    const expected = localIsoDate(new Date(sunday.getFullYear(), sunday.getMonth(), sunday.getDate() + 1 + index * 7));
    const loaded = buckets[index]?.weekLabel;
    if (!loaded || loaded !== expected) throw new Error('MPS business week changed or loaded week labels are incomplete. Reload the current view before exporting.');
    // Fiscal labels describe the authoritative loaded date, not a separately allocated date sequence.
    const info = getFiscalDisplayInfo(fiscal, parseIsoDateOnly(loaded));
    return `FY${info.fiscalYear} Q${info.fiscalQuarter} P${info.fiscalPeriod}`;
  });
  const request = analyticalRequest('MPS', assignmentId, dashboard.snapshot.snapshotId, dashboard.snapshot.site, localIsoDate(now));
  request.parent = null; request.dateBasis = basis; request.horizonWeeks = horizon; request.isStale = stale;
  request.fiscalLabels = labels;
  request.metadata = [{ label: 'Calendar', value: JSON.stringify(fiscal) }, { label: 'Export date', value: formatLocalDate(now) }];
  return request;
}

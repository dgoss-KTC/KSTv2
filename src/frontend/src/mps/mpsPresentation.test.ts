import { describe, it, expect } from 'vitest';
import type { MpsBucketDto, WorkOrderMaterialLineDto } from '../api/client';
import type { FiscalDisplayInfo } from '../fiscal/types';
import {
  bucketCellClassNames,
  describeBucket,
  describeBucketSelection,
  executionStatusClass,
  filterMaterialLinesByPart,
  formatQuantity,
  formatWeekLabel,
  groupConsecutive,
  isMaterialLineException,
  isWeeklyBucketWorkOrderEligible,
  materialLineDeparture,
  parseIsoDateOnly,
  sortMaterialLines,
  withPeriodColor,
  withQuarterColor,
  workOrderStatusLabel,
  WORK_ORDER_DRILLDOWN_HORIZON_WEEKS,
} from './mpsPresentation';

function makeMaterialLine(overrides: Partial<WorkOrderMaterialLineDto> = {}): WorkOrderMaterialLineDto {
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

function makeBucket(overrides: Partial<MpsBucketDto> = {}): MpsBucketDto {
  return {
    kind: 'weekly',
    weekLabel: '2025-06-30',
    quantity: 100,
    executionStatus: 'none',
    containsPlannedWork: false,
    containsExplicitlyScheduledWork: false,
    ...overrides,
  };
}

describe('groupConsecutive', () => {
  it('groups consecutive equal keys and keeps separate non-adjacent equal keys apart', () => {
    const items = [1, 1, 1, 2, 2, 1, 1];
    const groups = groupConsecutive(items, (n) => n);
    expect(groups).toEqual([
      { key: 1, span: 3 },
      { key: 2, span: 2 },
      { key: 1, span: 2 },
    ]);
  });

  it('returns an empty array for an empty input', () => {
    expect(groupConsecutive([], (n: number) => n)).toEqual([]);
  });

  it('returns one group per item when all keys differ', () => {
    const groups = groupConsecutive([1, 2, 3], (n) => n);
    expect(groups).toEqual([
      { key: 1, span: 1 },
      { key: 2, span: 1 },
      { key: 3, span: 1 },
    ]);
  });
});

describe('executionStatusClass', () => {
  it.each(['allocating', 'frozen', 'released', 'mixed'])('passes %s through as-is', (status) => {
    expect(executionStatusClass(status)).toBe(status);
  });

  it.each(['none', 'unknown', 'anything-else'])('maps %s to "none"', (status) => {
    expect(executionStatusClass(status)).toBe('none');
  });
});

describe('formatQuantity', () => {
  it('formats whole numbers without decimals', () => {
    expect(formatQuantity(1000)).toBe('1,000');
  });

  it('rounds to a maximum of 2 decimal places', () => {
    expect(formatQuantity(12.3456)).toBe('12.35');
  });

  it('returns the original string for non-numeric input', () => {
    expect(formatQuantity('n/a')).toBe('n/a');
  });
});

describe('formatWeekLabel', () => {
  it('returns an empty string for null', () => {
    expect(formatWeekLabel(null)).toBe('');
  });

  it('formats an ISO date as a short month/day label', () => {
    expect(formatWeekLabel('2025-06-30')).toBe('Jun 30');
  });
});

describe('parseIsoDateOnly', () => {
  it('parses an ISO date string into a local Date without a UTC day-shift', () => {
    const date = parseIsoDateOnly('2025-06-29');
    expect(date.getFullYear()).toBe(2025);
    expect(date.getMonth()).toBe(5);
    expect(date.getDate()).toBe(29);
  });
});

describe('describeBucket', () => {
  it('describes a plain bucket with just its status and quantity', () => {
    const description = describeBucket(makeBucket({ executionStatus: 'released', quantity: 250 }));
    expect(description).toBe('Released — 250 units');
  });

  it('includes planned-work and explicitly-scheduled markers when present', () => {
    const description = describeBucket(
      makeBucket({
        executionStatus: 'allocating',
        quantity: 10,
        containsPlannedWork: true,
        containsExplicitlyScheduledWork: true,
      }),
    );
    expect(description).toBe('Allocating, includes planned work, explicitly scheduled — 10 units');
  });

  it('falls back to the raw status string for unrecognized values', () => {
    const description = describeBucket(makeBucket({ executionStatus: 'weird-status' as never, quantity: 1 }));
    expect(description).toBe('weird-status — 1 units');
  });
});

describe('bucketCellClassNames', () => {
  it('includes the base cell class and mapped status class', () => {
    expect(bucketCellClassNames(makeBucket({ executionStatus: 'frozen' }))).toBe('mps-cell mps-cell--frozen');
  });

  it('maps an unrecognized status to the "none" status class', () => {
    expect(bucketCellClassNames(makeBucket({ executionStatus: 'unknown' }))).toBe('mps-cell mps-cell--none');
  });

  it('adds the planned marker class when containsPlannedWork is true', () => {
    const classNames = bucketCellClassNames(makeBucket({ containsPlannedWork: true }));
    expect(classNames).toBe('mps-cell mps-cell--none mps-cell--planned');
  });

  it('adds the explicit marker class when containsExplicitlyScheduledWork is true', () => {
    const classNames = bucketCellClassNames(makeBucket({ containsExplicitlyScheduledWork: true }));
    expect(classNames).toBe('mps-cell mps-cell--none mps-cell--explicit');
  });

  it('combines status, planned, and explicit classes together', () => {
    const classNames = bucketCellClassNames(
      makeBucket({ executionStatus: 'released', containsPlannedWork: true, containsExplicitlyScheduledWork: true }),
    );
    expect(classNames).toBe('mps-cell mps-cell--released mps-cell--planned mps-cell--explicit');
  });
});

function makeInfo(overrides: Partial<FiscalDisplayInfo> = {}): FiscalDisplayInfo {
  return { fiscalYear: 2027, fiscalWeek: 1, fiscalPeriod: 1, fiscalQuarter: 1, ...overrides };
}

describe('withQuarterColor', () => {
  it('annotates each group with the fiscal quarter number found at its starting index', () => {
    const infos = [makeInfo({ fiscalQuarter: 1 }), makeInfo({ fiscalQuarter: 1 }), makeInfo({ fiscalQuarter: 2 })];
    const groups = groupConsecutive(infos, (i) => i.fiscalQuarter);
    const bands = withQuarterColor(groups, infos);
    expect(bands).toEqual([
      { key: 1, span: 2, quarterNum: 1 },
      { key: 2, span: 1, quarterNum: 2 },
    ]);
  });

  it('defaults to quarter 1 when fiscal info is missing at a group start', () => {
    const groups = [{ key: 'n/a', span: 2 }];
    expect(withQuarterColor(groups, [null, null])).toEqual([{ key: 'n/a', span: 2, quarterNum: 1 }]);
  });
});

describe('withPeriodColor', () => {
  it('inherits the parent quarter number and alternates shade per period within that quarter', () => {
    const infos = [
      makeInfo({ fiscalQuarter: 1, fiscalPeriod: 1 }),
      makeInfo({ fiscalQuarter: 1, fiscalPeriod: 2 }),
      makeInfo({ fiscalQuarter: 1, fiscalPeriod: 3 }),
      makeInfo({ fiscalQuarter: 2, fiscalPeriod: 4 }),
    ];
    const groups = groupConsecutive(infos, (i) => `${i.fiscalYear}-P${i.fiscalPeriod}`);
    const bands = withPeriodColor(groups, infos);
    expect(bands).toEqual([
      { key: '2027-P1', span: 1, quarterNum: 1, altShade: false },
      { key: '2027-P2', span: 1, quarterNum: 1, altShade: true },
      { key: '2027-P3', span: 1, quarterNum: 1, altShade: false },
      { key: '2027-P4', span: 1, quarterNum: 2, altShade: false },
    ]);
  });

  it('defaults to quarter 1 and no alt shade when fiscal info is missing at a group start', () => {
    const groups = [{ key: 'n/a', span: 2 }];
    expect(withPeriodColor(groups, [null, null])).toEqual([
      { key: 'n/a', span: 2, quarterNum: 1, altShade: false },
    ]);
  });
});

describe('isWeeklyBucketWorkOrderEligible', () => {
  it('is eligible for the first WORK_ORDER_DRILLDOWN_HORIZON_WEEKS zero-based indices', () => {
    expect(WORK_ORDER_DRILLDOWN_HORIZON_WEEKS).toBe(6);
    for (let i = 0; i < WORK_ORDER_DRILLDOWN_HORIZON_WEEKS; i++) {
      expect(isWeeklyBucketWorkOrderEligible(i)).toBe(true);
    }
  });

  it('is not eligible at or beyond the configured horizon', () => {
    expect(isWeeklyBucketWorkOrderEligible(WORK_ORDER_DRILLDOWN_HORIZON_WEEKS)).toBe(false);
    expect(isWeeklyBucketWorkOrderEligible(WORK_ORDER_DRILLDOWN_HORIZON_WEEKS + 10)).toBe(false);
  });
});

describe('describeBucketSelection', () => {
  it('labels a Falldown selection', () => {
    expect(describeBucketSelection({ parentPart: 'ABC100', kind: 'falldown', weekLabel: null })).toBe('Falldown');
  });

  it('labels a weekly selection with its formatted week', () => {
    expect(
      describeBucketSelection({ parentPart: 'ABC100', kind: 'weekly', weekLabel: '2025-06-30' }),
    ).toBe('Week of Jun 30');
  });
});

describe('workOrderStatusLabel', () => {
  it('maps known Stage 7 status codes to their semantic label', () => {
    expect(workOrderStatusLabel('allocating')).toBe('Allocating');
    expect(workOrderStatusLabel('frozen')).toBe('Frozen');
    expect(workOrderStatusLabel('released')).toBe('Released');
  });

  it('falls back to the raw value for an unrecognized status', () => {
    expect(workOrderStatusLabel('unknown')).toBe('unknown');
  });
});

describe('materialLineDeparture', () => {
  it('returns the absolute distance from 100', () => {
    expect(materialLineDeparture(120)).toBe(20);
    expect(materialLineDeparture(80)).toBe(20);
    expect(materialLineDeparture(100)).toBe(0);
    expect(materialLineDeparture('95')).toBe(5);
  });

  it('returns null for a missing or non-numeric value', () => {
    expect(materialLineDeparture(null)).toBeNull();
    expect(materialLineDeparture(undefined)).toBeNull();
  });
});

describe('isMaterialLineException', () => {
  it('treats under- and over-issued as exceptions', () => {
    expect(isMaterialLineException('underIssuedException')).toBe(true);
    expect(isMaterialLineException('overIssuedException')).toBe(true);
  });

  it('treats within-range and missing status as non-exceptions', () => {
    expect(isMaterialLineException('withinExpectedRange')).toBe(false);
    expect(isMaterialLineException(null)).toBe(false);
  });
});

describe('sortMaterialLines', () => {
  it('sorts larger departures from 100% ahead of smaller ones, exceptions first', () => {
    const lines = [
      makeMaterialLine({ componentPart: 'NORMAL', issuedPercent: 98, issueStatus: 'withinExpectedRange' }),
      makeMaterialLine({ componentPart: 'OVER', issuedPercent: 160, issueStatus: 'overIssuedException' }),
      makeMaterialLine({ componentPart: 'UNDER', issuedPercent: 50, issueStatus: 'underIssuedException' }),
      makeMaterialLine({ componentPart: 'EXACT', issuedPercent: 100, issueStatus: 'withinExpectedRange' }),
    ];
    const sorted = sortMaterialLines(lines).map((line) => line.componentPart);
    expect(sorted).toEqual(['OVER', 'UNDER', 'NORMAL', 'EXACT']);
  });

  it('breaks ties deterministically by component part, then original position for true duplicates', () => {
    const lines = [
      makeMaterialLine({ componentPart: 'B', issuedPercent: 100 }),
      makeMaterialLine({ componentPart: 'A', issuedPercent: 100 }),
      makeMaterialLine({ componentPart: 'A', issuedPercent: 100 }),
    ];
    const sorted = sortMaterialLines(lines);
    expect(sorted.map((line) => line.componentPart)).toEqual(['A', 'A', 'B']);
  });

  it('sorts lines with an unknown Issued % last, without throwing', () => {
    const lines = [
      makeMaterialLine({ componentPart: 'KNOWN', issuedPercent: 50, issueStatus: 'underIssuedException' }),
      makeMaterialLine({ componentPart: 'UNKNOWN', issuedPercent: null, issueStatus: null }),
    ];
    expect(sortMaterialLines(lines).map((line) => line.componentPart)).toEqual(['KNOWN', 'UNKNOWN']);
  });

  it('does not mutate the input array', () => {
    const lines = [makeMaterialLine({ componentPart: 'B' }), makeMaterialLine({ componentPart: 'A' })];
    const original = [...lines];
    sortMaterialLines(lines);
    expect(lines).toEqual(original);
  });
});

describe('filterMaterialLinesByPart', () => {
  const lines = [makeMaterialLine({ componentPart: 'ABC-100' }), makeMaterialLine({ componentPart: 'xyz-200' })];

  it('returns all lines for an empty or whitespace-only query', () => {
    expect(filterMaterialLinesByPart(lines, '')).toHaveLength(2);
    expect(filterMaterialLinesByPart(lines, '   ')).toHaveLength(2);
  });

  it('matches case-insensitively and partially', () => {
    expect(filterMaterialLinesByPart(lines, 'abc').map((l) => l.componentPart)).toEqual(['ABC-100']);
    expect(filterMaterialLinesByPart(lines, 'XYZ').map((l) => l.componentPart)).toEqual(['xyz-200']);
    expect(filterMaterialLinesByPart(lines, '-100').map((l) => l.componentPart)).toEqual(['ABC-100']);
  });

  it('returns an empty array when nothing matches', () => {
    expect(filterMaterialLinesByPart(lines, 'none-such')).toEqual([]);
  });
});

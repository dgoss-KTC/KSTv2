import { describe, expect, it } from 'vitest';
import type { components } from '../generated/api';
import {
  classifyPoDueState,
  describeComponentOrderDescription,
  EMPTY_COMPONENT_ORDERS_FILTERS,
  filterComponentOrderGroups,
  formatKssIndicator,
  formatLineConfirmation,
  formatWeeksLeadTime,
  getPoDueDateCutoff,
  MISSING_MASTER_DESCRIPTION,
  poDueStateClass,
} from './componentOrdersPresentation';

function dateOnly(year: number, month1Based: number, day: number): Date {
  return new Date(year, month1Based - 1, day);
}

describe('getPoDueDateCutoff', () => {
  it('uses that week Friday for Monday through Friday (business week starts Sunday)', () => {
    // 2026-09-07 is a Monday; its business week runs Sun 2026-09-06 .. Sat 2026-09-12.
    expect(getPoDueDateCutoff(dateOnly(2026, 9, 7)).toDateString()).toBe(dateOnly(2026, 9, 11).toDateString());
    // Wednesday of the same week keeps the same Friday.
    expect(getPoDueDateCutoff(dateOnly(2026, 9, 9)).toDateString()).toBe(dateOnly(2026, 9, 11).toDateString());
    // Friday itself uses its own day (due on or before it is late).
    expect(getPoDueDateCutoff(dateOnly(2026, 9, 11)).toDateString()).toBe(dateOnly(2026, 9, 11).toDateString());
  });

  it('retains the just-passed Friday on Saturday and Sunday (never rolls forward)', () => {
    // Saturday 2026-09-12 retains Friday 2026-09-11.
    expect(getPoDueDateCutoff(dateOnly(2026, 9, 12)).toDateString()).toBe(dateOnly(2026, 9, 11).toDateString());
    // Sunday 2026-09-13 (new business week) still retains Friday 2026-09-11.
    expect(getPoDueDateCutoff(dateOnly(2026, 9, 13)).toDateString()).toBe(dateOnly(2026, 9, 11).toDateString());
  });

  it('handles a month boundary without rolling forward on Sunday', () => {
    // 2026-10-04 is a Sunday; the just-passed Friday is 2026-10-02.
    expect(getPoDueDateCutoff(dateOnly(2026, 10, 4)).toDateString()).toBe(dateOnly(2026, 10, 2).toDateString());
  });
});

describe('classifyPoDueState', () => {
  const monday = dateOnly(2026, 9, 7); // cutoff Friday 2026-09-11

  it('classifies missing/null due dates as the yellow exception state', () => {
    expect(classifyPoDueState(null, monday)).toBe('missing');
    expect(classifyPoDueState(undefined, monday)).toBe('missing');
  });

  it('is late when due on or before the Friday cutoff and normal after it', () => {
    expect(classifyPoDueState('2026-09-11', monday)).toBe('late'); // exactly the cutoff
    expect(classifyPoDueState('2026-08-30', monday)).toBe('late'); // already past
    expect(classifyPoDueState('2026-09-12', monday)).toBe('normal'); // just after
  });

  it('uses the retained Friday on weekend days', () => {
    const saturday = dateOnly(2026, 9, 12);
    expect(classifyPoDueState('2026-09-11', saturday)).toBe('late');
    expect(classifyPoDueState('2026-09-12', saturday)).toBe('normal');
  });
});

describe('poDueStateClass', () => {
  it('maps states to CSS classes and normal to none', () => {
    expect(poDueStateClass('missing')).toBe('component-orders__due--missing');
    expect(poDueStateClass('late')).toBe('component-orders__due--late');
    expect(poDueStateClass('normal')).toBe('');
  });
});

describe('formatWeeksLeadTime', () => {
  it('shows "-" for null and zero days', () => {
    expect(formatWeeksLeadTime(null)).toBe('-');
    expect(formatWeeksLeadTime(undefined)).toBe('-');
    expect(formatWeeksLeadTime(0)).toBe('-');
  });

  it('displays ceil(days / 7) otherwise', () => {
    expect(formatWeeksLeadTime(14)).toBe('2');
    expect(formatWeeksLeadTime(8)).toBe('2'); // ceil(8/7) = 2
    expect(formatWeeksLeadTime(7)).toBe('1');
    expect(formatWeeksLeadTime(1)).toBe('1');
    expect(formatWeeksLeadTime(30)).toBe('5');
  });
});

describe('description, confirmation and KSS display', () => {
  it('shows Missing master data when no part-master description exists', () => {
    expect(describeComponentOrderDescription(null)).toBe(MISSING_MASTER_DESCRIPTION);
    expect(describeComponentOrderDescription(undefined)).toBe(MISSING_MASTER_DESCRIPTION);
    expect(describeComponentOrderDescription('Hex bolt M8')).toBe('Hex bolt M8');
  });

  it('renders Confirmed as Yes/No and a null source value blank', () => {
    expect(formatLineConfirmation(true)).toBe('Yes');
    expect(formatLineConfirmation(false)).toBe('No');
    expect(formatLineConfirmation(null)).toBe('');
    expect(formatLineConfirmation(undefined)).toBe('');
  });

  it('renders the KSS indicator as "KSS" or blank', () => {
    expect(formatKssIndicator(true)).toBe('KSS');
    expect(formatKssIndicator(false)).toBe('');
  });
});

describe('filterComponentOrderGroups', () => {
  const line = (overrides: Partial<components['schemas']['ComponentOrderLineDto']> = {}): components['schemas']['ComponentOrderLineDto'] => ({
    componentPart: 'P1',
    description: null,
    leadTimeDays: null,
    poNumber: '2076185',
    poLine: 1,
    dueDate: null,
    openQuantity: 10,
    confirmed: true,
    supplierDisplay: 'ACME',
    buyerDisplay: null,
    manufacturerItem: null,
    isKss: false,
    trackingInfo: null,
    isCreditHold: null,
    isCia: null,
    currentComments: null,
    ...overrides,
  });

  const group = (part: string, displayLine: components['schemas']['ComponentOrderLineDto']): components['schemas']['ComponentOrderGroupDto'] => ({
    componentPart: part,
    displayLine,
    additionalLines: [],
  });

  it('matches the component number case-insensitively as a partial match', () => {
    const groups = [group('ABC-100', line({ componentPart: 'ABC-100' })), group('xyz', line({ componentPart: 'xyz' }))];
    expect(filterComponentOrderGroups(groups, { ...EMPTY_COMPONENT_ORDERS_FILTERS, componentNumber: 'abc' })).toHaveLength(1);
    expect(filterComponentOrderGroups(groups, EMPTY_COMPONENT_ORDERS_FILTERS)).toHaveLength(2);
  });

  it('keeps only KSS components when the KSS-only filter is on', () => {
    const kssGroup = group('K1', line({ componentPart: 'K1', isKss: true }));
    const plainGroup = group('P1', line({ componentPart: 'P1', isKss: false }));
    expect(filterComponentOrderGroups([kssGroup, plainGroup], { ...EMPTY_COMPONENT_ORDERS_FILTERS, kssOnly: true }))
      .toEqual([kssGroup]);
  });

  it('combines both delivered filters', () => {
    const kssMatch = group('K1', line({ componentPart: 'K1', isKss: true }));
    const kssNoMatch = group('Z9', line({ componentPart: 'Z9', isKss: true }));
    const plainMatch = group('P1', line({ componentPart: 'P1', isKss: false }));
    expect(filterComponentOrderGroups([kssMatch, kssNoMatch, plainMatch], {
      ...EMPTY_COMPONENT_ORDERS_FILTERS,
      componentNumber: 'k',
      kssOnly: true,
    }))
      .toEqual([kssMatch]);
  });
});

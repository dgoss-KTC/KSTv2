import { describe, it, expect } from 'vitest';
import type { MpsBucketDto } from '../api/client';
import {
  describeBucket,
  executionStatusClass,
  formatQuantity,
  formatWeekLabel,
  groupConsecutive,
  parseIsoDateOnly,
} from './mpsPresentation';

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

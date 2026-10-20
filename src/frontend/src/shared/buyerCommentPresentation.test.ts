import { describe, expect, it } from 'vitest';
import { formatBuyerCommentMetadata } from './buyerCommentPresentation';

describe('source wall-clock comment metadata', () => {
  it('preserves source date/time with no timezone interpretation', () => {
    expect(formatBuyerCommentMetadata('2022-01-31T13:30:00', 'jdoe')).toBe('Jan 31, 2022 · 1:30 PM · jdoe');
    expect(formatBuyerCommentMetadata('2022-01-31T00:05:00', 'jdoe')).toBe('Jan 31, 2022 · 12:05 AM · jdoe');
    expect(formatBuyerCommentMetadata('2022-01-31T12:00:00.0000000', 'jdoe')).toBe('Jan 31, 2022 · 12:00 PM · jdoe');
  });
  it('does not pretend acquisition timestamps, malformed strings or UTC instants are source comment time', () => {
    for (const input of [null, undefined, '', 'invalid', '2022-01-31T13:30:00Z', '2022-01-31T13:30:00-07:00']) {
      expect(formatBuyerCommentMetadata(input, null)).toBe('Date unknown');
    }
    expect(formatBuyerCommentMetadata(null, 'jdoe')).toBe('Date unknown · jdoe');
  });
});

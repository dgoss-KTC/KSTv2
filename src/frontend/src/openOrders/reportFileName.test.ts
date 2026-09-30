import { describe, expect, it } from 'vitest';
import { openOrdersFileName } from './reportFileName';

describe('Open Orders suggested filename fallback', () => {
  it.each([
    ['Shure SMT', 'SW', 'Shure-SMT'],
    [' Shure / SMT..  ', 'SW', 'Shure-SMT'],
    ['A\\B:C*D?E"F<G>H|I', 'SW', 'A-B-C-D-E-F-G-H-I'],
    ['Acme\u0085West', 'SW', 'Acme-West'],
    ['  Acme___  -- West...  ', 'SW', 'Acme-West'],
    ['\u0001\t:/*? . ', 'SW', 'SW'],
    [null, 'SW', 'SW'],
    ['', '  .  ', 'Workspace'],
  ])('normalizes %s and falls back to site %s', (name, site, prefix) => {
    expect(openOrdersFileName(name, site, '2026-09-30T10:00:00Z')).toBe(`${prefix}-Open-Orders-2026-09-30.xlsx`);
  });
});

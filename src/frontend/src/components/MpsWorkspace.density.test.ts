import { describe, it, expect } from 'vitest';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const mpsWorkspaceCss = readFileSync(resolve(process.cwd(), 'src/components/MpsWorkspace.css'), 'utf8');
const indexCss = readFileSync(resolve(process.cwd(), 'src/index.css'), 'utf8');

/**
 * jsdom does not execute the real CSS cascade (getComputedStyle does not resolve values from
 * imported stylesheets), so density's visual effect cannot be proven via rendered computed
 * style in tests. These tests instead assert the source-of-truth wiring directly: that the MPS
 * grid actually consumes the shared `--density-*` custom properties (previously it did not,
 * which was the reported bug), and that compact/comfortable define distinct values for them.
 */
describe('MPS grid density wiring', () => {
  it('ties data-row height to the shared density preference', () => {
    expect(mpsWorkspaceCss).toMatch(/\.mps-grid tbody td\s*{[^}]*height:\s*var\(--density-row-height\)/);
  });

  it('ties week-column width to the shared density preference', () => {
    expect(mpsWorkspaceCss).toMatch(/\.mps-grid__week-col\s*{[^}]*width:\s*var\(--density-week-col-width\)/);
  });

  it('ties grid cell padding to the shared density preference', () => {
    expect(mpsWorkspaceCss).toMatch(
      /padding:\s*var\(--density-cell-padding-y\)\s*var\(--density-cell-padding-x\)/,
    );
  });

  it('ties header/toolbar control height to the shared density preference', () => {
    expect(mpsWorkspaceCss).toMatch(/height:\s*var\(--density-control-height\)/);
  });
});

describe('density tokens (index.css)', () => {
  function tokenValue(block: RegExp, token: string): string | undefined {
    const blockMatch = block.exec(indexCss);
    if (!blockMatch) return undefined;
    const tokenMatch = new RegExp(`${token}:\\s*(\\d+)px`).exec(blockMatch[0]);
    return tokenMatch?.[1];
  }

  const rootBlock = /:root\s*{[^}]*}/;
  const comfortableBlock = /\[data-density='comfortable'\]\s*{[^}]*}/;

  it('defines a compact row height near the ~32px guidance', () => {
    expect(tokenValue(rootBlock, '--density-row-height')).toBe('32');
  });

  it('defines a comfortable row height near the ~42px guidance', () => {
    expect(tokenValue(comfortableBlock, '--density-row-height')).toBe('42');
  });

  it('defines a compact week-column width near the ~78px guidance', () => {
    expect(tokenValue(rootBlock, '--density-week-col-width')).toBe('78');
  });

  it('defines a comfortable week-column width near the ~92px guidance', () => {
    expect(tokenValue(comfortableBlock, '--density-week-col-width')).toBe('92');
  });

  it('gives compact and comfortable distinct cell padding', () => {
    const compact = tokenValue(rootBlock, '--density-cell-padding-y');
    const comfortable = tokenValue(comfortableBlock, '--density-cell-padding-y');
    expect(compact).toBeDefined();
    expect(comfortable).toBeDefined();
    expect(compact).not.toBe(comfortable);
  });
});

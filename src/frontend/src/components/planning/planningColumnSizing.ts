/**
 * Content-sized Planning value columns (owner amendment 2026-10-06). All week columns (Falldown
 * included) share one width and all quarter-total columns share another. Each width is the widest
 * rendered primary value of its group (measured from the actual rendered text and font metrics, plus
 * the cell's padding and borders), never below the density minimum. The struck-through loaded value
 * beside a changed value is the only optional content: it is shown when it fits next to the complete
 * new value and is otherwise hidden (the tooltip keeps it). Nothing here changes a value or a format.
 */

export interface ColumnMinimums { week: number; quarter: number }

/** Compact minimums; comfortable (88 / 116) comes from the CSS tokens for the active density. */
export const FALLBACK_MINIMUMS: ColumnMinimums = { week: 80, quarter: 108 };

/** One shared width for a group: the widest requirement rounded up, never below the minimum. */
export function sharedColumnWidth(requirements: readonly number[], minimum: number): number {
  let widest = 0;
  for (const r of requirements) if (Number.isFinite(r) && r > widest) widest = r;
  return Math.max(minimum, Math.ceil(widest));
}

/** Whether the struck loaded value (including its gap) fits beside the complete new value. */
export function strikeFits(textWidth: number, wasWidthWithGap: number, available: number): boolean {
  return textWidth + wasWidthWithGap <= available + 0.5;
}

/** Selector list highlighting the hovered column (`data-hover-col` = cell index, 1-based data columns). */
export function hoverColumnCss(cellsPerRow: number): string {
  if (cellsPerRow < 2) return '';
  const selectors: string[] = [];
  for (let i = 1; i < cellsPerRow; i++) selectors.push(`.pwb-grid-scroll[data-hover-col="${i}"] .pwb-grid tr > :nth-child(${i + 1})`);
  return `${selectors.join(',\n')} { --pwb-hover: var(--pwb-hover-wash); }`;
}

const px = (value: string) => {
  const n = parseFloat(value);
  return Number.isFinite(n) ? n : 0;
};

/** Horizontal padding plus borders of an element, from its computed style. */
function horizontalChrome(style: CSSStyleDeclaration): number {
  return px(style.paddingLeft) + px(style.paddingRight) + px(style.borderLeftWidth) + px(style.borderRightWidth);
}

/** Intrinsic rendered width of an element's text (block labels included). */
function textWidth(el: Element): number {
  const range = typeof document.createRange === 'function' ? document.createRange() : null;
  if (range && typeof range.getBoundingClientRect === 'function') {
    range.selectNodeContents(el);
    return range.getBoundingClientRect().width;
  }
  return el.getBoundingClientRect().width;
}

interface Strike { s: HTMLElement; text: number; was: number; group: 'week' | 'quarter'; chrome: number }

/**
 * Measures the rendered grid and applies the shared widths as `--pwb-col-w` / `--pwb-qtr-w` on the
 * table, then shows or hides each struck loaded value. Reads are batched before writes. Safe to call
 * repeatedly: text widths do not depend on the column widths, so the result is stable.
 */
export function sizePlanningColumns(table: HTMLTableElement | null, measure: HTMLElement | null): void {
  if (!table?.isConnected) return;
  // Struck values are measured visible; their final visibility is decided below.
  const strikes = table.querySelectorAll<HTMLElement>('s.pwb-was');
  strikes.forEach(s => { if (s.hidden) s.hidden = false; });

  const tableStyle = getComputedStyle(table);
  const minimums: ColumnMinimums = {
    week: px(tableStyle.getPropertyValue('--pwb-col-min')) || FALLBACK_MINIMUMS.week,
    quarter: px(tableStyle.getPropertyValue('--pwb-qtr-min')) || FALLBACK_MINIMUMS.quarter,
  };
  const required = { week: [] as number[], quarter: [] as number[] };
  const pending: Strike[] = [];
  // Cells with the same classes share padding and borders: read each computed style once per pass.
  const chromeByClass = new Map<string, number>();
  const chromeOf = (el: Element) => {
    let value = chromeByClass.get(el.className);
    if (value === undefined) { value = horizontalChrome(getComputedStyle(el)); chromeByClass.set(el.className, value); }
    return value;
  };
  let inputStyle: CSSStyleDeclaration | null = null;
  let wasGap: number | null = null;

  const cells = table.querySelectorAll<HTMLTableCellElement>('.pwb-head--week, .pwb-head--falldown, .pwb-head--quarter, .pwb-cell--week, .pwb-cell--falldown, .pwb-cell--quarter');
  cells.forEach(cell => {
    const group = cell.classList.contains('pwb-head--quarter') || cell.classList.contains('pwb-cell--quarter') ? 'quarter' : 'week';
    const chrome = chromeOf(cell);
    let content = 0;
    const input = cell.querySelector<HTMLInputElement>('input.pwb-adjust');
    if (input) {
      if (input.value !== '' && measure) {
        if (!inputStyle) {
          inputStyle = getComputedStyle(input);
          measure.style.font = inputStyle.font;
          measure.style.fontVariantNumeric = inputStyle.fontVariantNumeric;
          measure.style.letterSpacing = inputStyle.letterSpacing;
        }
        measure.textContent = input.value;
        // Text, the caret, the input's own padding and borders.
        content = measure.getBoundingClientRect().width + 2 + chromeOf(input);
      }
    } else {
      cell.querySelectorAll('.pwb-v, .pwb-head__label, .pwb-head__sub').forEach(el => { content = Math.max(content, textWidth(el)); });
      const s = cell.querySelector<HTMLElement>('s.pwb-was');
      const v = cell.querySelector('.pwb-v');
      if (s && v) {
        if (wasGap === null) wasGap = px(getComputedStyle(s).marginRight);
        pending.push({ s, text: textWidth(v), was: s.getBoundingClientRect().width + wasGap, group, chrome });
      }
    }
    required[group].push(content + chrome);
  });
  if (measure) measure.textContent = '';

  const week = sharedColumnWidth(required.week, minimums.week);
  const quarter = sharedColumnWidth(required.quarter, minimums.quarter);
  if (table.style.getPropertyValue('--pwb-col-w') !== `${week}px`) table.style.setProperty('--pwb-col-w', `${week}px`);
  if (table.style.getPropertyValue('--pwb-qtr-w') !== `${quarter}px`) table.style.setProperty('--pwb-qtr-w', `${quarter}px`);
  for (const p of pending) {
    const available = (p.group === 'quarter' ? quarter : week) - p.chrome;
    p.s.hidden = !strikeFits(p.text, p.was, available);
  }
}

/**
 * Exact decimal arithmetic on API decimal strings (Stage 13 origin, shared from Stage 14.4).
 * Values are parsed into a bigint coefficient and a decimal scale. Quantities, prices and dollars
 * are never converted to JavaScript Number for comparisons or arithmetic.
 */
export interface ExactDecimal { n: bigint; scale: number }

export function decimal(value: string): ExactDecimal | null {
  if (!/^[+-]?(?:\d+(?:\.\d*)?|\.\d+)$/.test(value)) return null;
  const negative = value.startsWith('-');
  const digits = value.replace(/^[+-]/, '');
  const [whole, fraction = ''] = digits.split('.');
  return { n: BigInt(`${negative ? '-' : ''}${whole || '0'}${fraction}`), scale: fraction.length };
}

/** Canonical exact text: no exponent, no trailing fractional zeros, "-0" normalized to "0". */
export function exactText(value: ExactDecimal): string {
  const sign = value.n < 0n ? '-' : '';
  const digits = (value.n < 0n ? -value.n : value.n).toString().padStart(value.scale + 1, '0');
  if (!value.scale) return sign + digits;
  const fraction = digits.slice(-value.scale).replace(/0+$/, '');
  return `${sign}${digits.slice(0, -value.scale)}${fraction ? `.${fraction}` : ''}`;
}

function aligned(a: ExactDecimal, b: ExactDecimal): [bigint, bigint, number] {
  const scale = Math.max(a.scale, b.scale);
  return [a.n * 10n ** BigInt(scale - a.scale), b.n * 10n ** BigInt(scale - b.scale), scale];
}

export function add(a: string, b: string): string | null {
  const left = decimal(a); const right = decimal(b);
  if (!left || !right) return null;
  const [x, y, scale] = aligned(left, right);
  return exactText({ n: x + y, scale });
}

export function subtract(a: string, b: string): string | null {
  const left = decimal(a); const right = decimal(b);
  if (!left || !right) return null;
  const [x, y, scale] = aligned(left, right);
  return exactText({ n: x - y, scale });
}

export function multiply(a: string, b: string): string | null {
  const left = decimal(a); const right = decimal(b);
  return left && right ? exactText({ n: left.n * right.n, scale: left.scale + right.scale }) : null;
}

export function equalDecimal(a: string, b: string): boolean {
  return decimal(a) !== null && subtract(a, b) === '0';
}

/** Exact sum of decimal strings; "0" for an empty list. Throws on non-decimal text rather than guessing. */
export function sumDecimals(values: readonly string[]): string {
  let total = '0';
  for (const value of values) {
    const next = add(total, value);
    if (next === null) throw new Error('Non-decimal value in exact sum.');
    total = next;
  }
  return total;
}

export function isNegativeDecimal(value: string): boolean {
  const parsed = decimal(value);
  return parsed !== null && parsed.n < 0n;
}

/**
 * Presentation only: rounds half away from zero to `places` decimals and groups thousands. The exact
 * value is never replaced; callers keep the original string for any calculation.
 */
export function formatDecimal(value: string, places: number | null): string {
  const parsed = decimal(value);
  if (!parsed) return value;
  const negative = parsed.n < 0n;
  let absolute = negative ? -parsed.n : parsed.n;
  let scale = parsed.scale;
  if (places !== null && scale > places) {
    const divisor = 10n ** BigInt(scale - places);
    absolute = (absolute + divisor / 2n) / divisor;
    scale = places;
  }
  let text = exactText({ n: absolute, scale });
  if (places !== null) {
    const [, fraction = ''] = text.split('.');
    text = places === 0 ? text : `${text.split('.')[0]}.${fraction.padEnd(places, '0')}`;
  }
  const [whole, fraction] = text.split('.');
  const grouped = whole.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
  const isZero = absolute === 0n;
  return `${negative && !isZero ? '-' : ''}${grouped}${fraction !== undefined ? `.${fraction}` : ''}`;
}

import { formatDecimal } from '../shared/exactDecimal';

export function formatWorkspaceShipmentsOnTimeRate(
  numerator: string | number | null,
  denominator: string | number | null,
): string {
  if (numerator === null || denominator === null) return '—';
  const n = BigInt(numerator);
  const d = BigInt(denominator);
  if (d <= 0n) return '—';
  const tenthsPercent = (n * 1000n + d / 2n) / d;
  return `${formatDecimal((tenthsPercent / 10n).toString(), 0)}.${tenthsPercent % 10n}%`;
}

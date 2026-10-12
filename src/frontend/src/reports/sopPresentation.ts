import type { components } from '../generated/api';
import { getFiscalDisplayInfo, getPeriodWeekSpan } from '../fiscal/fiscalCalendar';
import { loadFiscalCalendarSettings } from '../fiscal/fiscalCalendarSettings';
import type { FiscalCalendarSettings } from '../fiscal/types';

export type SopPresentation = components['schemas']['SopPresentationDto'];
export const fiscalStorageKey = 'kst.fiscalCalendarSettings.v1';
export const fiscalChangedEvent = 'kst:fiscal-calendar-changed';

function date(iso: string): Date {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(iso)) throw new Error('Invalid fiscal calendar date.');
  const [y, m, d] = iso.split('-').map(Number);
  const result = new Date(y, m - 1, d);
  if (result.getFullYear() !== y || result.getMonth() !== m - 1 || result.getDate() !== d || y < 1900 || y > 2200) throw new Error('Invalid fiscal calendar date.');
  return result;
}
function iso(d: Date) { return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; }

export function sopCalendarSettings(): FiscalCalendarSettings {
  // Load the actual settings, but reject invalid persisted content rather than silently sanitizing
  // an S&OP calendar. Missing settings use the established default, never an inferred exception.
  const raw = localStorage.getItem(fiscalStorageKey);
  const settings = raw === null ? loadFiscalCalendarSettings() : JSON.parse(raw) as FiscalCalendarSettings;
  if (!settings || !Number.isInteger(settings.anchorFiscalYear) || settings.anchorFiscalYear < 1900 || settings.anchorFiscalYear > 2200
    || date(settings.anchorStartDate).getDay() !== 0 || !Array.isArray(settings.exceptions) || settings.exceptions.length > 100
    || settings.exceptions.some(e => !e || !Number.isInteger(e.fiscalYear) || e.fiscalYear < 1900 || e.fiscalYear > 2200
      || !Number.isInteger(e.extraWeekPeriod) || e.extraWeekPeriod < 1 || e.extraWeekPeriod > 12)
    || new Set(settings.exceptions.map(e => e.fiscalYear)).size !== settings.exceptions.length) throw new Error('Invalid fiscal calendar settings. Review Settings before Generate.');
  return settings;
}
export function sopCalendarFingerprint() { return JSON.stringify(sopCalendarSettings()); }
export function sopPresentation(weekLabels: readonly string[], settings = sopCalendarSettings()): SopPresentation {
  if (weekLabels.length < 1 || weekLabels.length > 156) throw new Error('Invalid S&OP week coverage.');
  const periods: components['schemas']['SopPeriodDto'][] = [];
  weekLabels.forEach((label, index) => {
    const d = date(label);
    if (d.getDay() !== 1 || (index > 0 && d.getTime() - date(weekLabels[index - 1]).getTime() < 6 * 86400000)
      || (index > 0 && iso(new Date(d.getFullYear(), d.getMonth(), d.getDate() - 7)) !== weekLabels[index - 1])) throw new Error('Invalid S&OP week coverage.');
    const info = getFiscalDisplayInfo(settings, d);
    const last = periods[periods.length - 1];
    if (last?.fiscalYear === info.fiscalYear && last.period === info.fiscalPeriod) { last.weekLabels.push(label); return; }
    const span = getPeriodWeekSpan(settings, info.fiscalYear, info.fiscalPeriod);
    periods.push({ fiscalYear: info.fiscalYear, period: info.fiscalPeriod, quarter: info.fiscalQuarter,
      startSunday: iso(span.startDate), weekCount: span.weekCount, weekLabels: [label] });
  });
  return { anchorFiscalYear: settings.anchorFiscalYear, anchorStartDate: settings.anchorStartDate, exceptions: settings.exceptions, periods };
}

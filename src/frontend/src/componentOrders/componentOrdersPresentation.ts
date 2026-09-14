import type { components } from '../generated/api';
import { parseIsoDateOnly } from '../mps/mpsPresentation';

/** PO due-date presentation state for a Component Orders row. */
export type PoDueDateState = 'missing' | 'late' | 'normal';

function addDays(date: Date, days: number): Date {
  const result = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  result.setDate(result.getDate() + days);
  return result;
}

/** The Sunday that begins the business week containing `date` (business weeks run Sun–Sat). */
function startOfBusinessWeek(date: Date): Date {
  const local = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  local.setDate(local.getDate() - local.getDay());
  return local;
}

/**
 * The current workweek's Friday cutoff for PO due-date highlighting. Monday–Friday use that
 * week's Friday (the business week starting the preceding Sunday). Saturday and Sunday retain
 * the Friday that just passed — they never roll forward to the following Friday.
 */
export function getPoDueDateCutoff(today: Date): Date {
  const day = today.getDay(); // 0=Sun .. 6=Sat
  if (day >= 1 && day <= 5) {
    return addDays(startOfBusinessWeek(today), 5);
  }
  const yesterday = addDays(today, -1);
  return addDays(startOfBusinessWeek(yesterday), 5);
}

/**
 * Classifies one row's PO due date: missing/null is a yellow exception; due on or before the
 * current workweek's Friday cutoff is red/late; everything after the cutoff is normal. The
 * collapsed component row reflects its displayed earliest line; child rows use their own dates.
 */
export function classifyPoDueState(dueDateIso: string | null | undefined, today: Date): PoDueDateState {
  if (!dueDateIso) return 'missing';
  const due = parseIsoDateOnly(dueDateIso);
  if (Number.isNaN(due.getTime())) return 'normal';
  return due.getTime() <= getPoDueDateCutoff(today).getTime() ? 'late' : 'normal';
}

/** CSS class for a PO due-date state cell (see ComponentOrdersPanel.css). */
export function poDueStateClass(state: PoDueDateState): string {
  switch (state) {
    case 'missing':
      return 'component-orders__due--missing';
    case 'late':
      return 'component-orders__due--late';
    default:
      return '';
  }
}

/** Weeks LT display: "-" when resolved days are zero or null, otherwise ceil(days / 7). */
export function formatWeeksLeadTime(days: string | number | null | undefined): string {
  if (days === null || days === undefined) return '-';
  const value = Number(days);
  if (!Number.isFinite(value) || value <= 0) return '-';
  return String(Math.ceil(value / 7));
}

export const MISSING_MASTER_DESCRIPTION = 'Missing master data';

/** Component Description display: the part-master text, or the missing-master state when absent. */
export function describeComponentOrderDescription(description: string | null | undefined): string {
  return description ?? MISSING_MASTER_DESCRIPTION;
}

/** Line-level Confirmed display: Yes/No from the line fact; a null source value renders blank (never fabricated). */
export function formatLineConfirmation(confirmed: boolean | null | undefined): string {
  if (confirmed === true) return 'Yes';
  if (confirmed === false) return 'No';
  return '';
}

/** KSS indicator display: `KSS` when the independent effective supplier-schedule relationship holds, else blank. */
export function formatKssIndicator(isKss: boolean): string {
  return isKss ? 'KSS' : '';
}

/**
 * Component Orders filter state: component number (case-insensitive partial match), KSS, and
 * risk facts on the displayed qualifying PO line.
 */
export interface ComponentOrdersFilters {
  componentNumber: string;
  kssOnly: boolean;
  creditHoldOnly: boolean;
  ciaOnly: boolean;
}

export const EMPTY_COMPONENT_ORDERS_FILTERS: ComponentOrdersFilters = {
  componentNumber: '',
  kssOnly: false,
  creditHoldOnly: false,
  ciaOnly: false,
};

/** Applies the delivered filters to already-loaded groups (frontend-local; never re-queries QAD). */
export function filterComponentOrderGroups(
  groups: readonly components['schemas']['ComponentOrderGroupDto'][],
  filters: ComponentOrdersFilters,
): components['schemas']['ComponentOrderGroupDto'][] {
  const needle = filters.componentNumber.trim().toLowerCase();
  return groups.filter((group) => {
    if (needle && !group.componentPart.toLowerCase().includes(needle)) return false;
    // KSS is a per-component+site relationship, so every line in one group shares the indicator.
    if (filters.kssOnly && !group.displayLine.isKss) return false;
    if (filters.creditHoldOnly && group.displayLine.isCreditHold !== true) return false;
    if (filters.ciaOnly && group.displayLine.isCia !== true) return false;
    return true;
  });
}

import type { MpsDateBasis } from '../api/mpsApi';

const STORAGE_KEY = 'kst.mpsDisplayPreferences.v1';

export const MIN_MPS_HORIZON_WEEKS = 1;
export const MAX_MPS_HORIZON_WEEKS = 72;
export const DEFAULT_MPS_HORIZON_WEEKS = 12;

export interface MpsDisplayPreferences {
  horizonWeeks: number;
  dateBasis: MpsDateBasis;
}

export const DEFAULT_MPS_DISPLAY_PREFERENCES: MpsDisplayPreferences = {
  horizonWeeks: DEFAULT_MPS_HORIZON_WEEKS,
  dateBasis: 'dueDate',
};

function isMpsDateBasis(value: unknown): value is MpsDateBasis {
  return value === 'dueDate' || value === 'releaseDate';
}

function isValidHorizon(value: unknown): value is number {
  return (
    typeof value === 'number' &&
    Number.isInteger(value) &&
    value >= MIN_MPS_HORIZON_WEEKS &&
    value <= MAX_MPS_HORIZON_WEEKS
  );
}

function normalize(value: unknown): MpsDisplayPreferences {
  if (typeof value !== 'object' || value === null) return DEFAULT_MPS_DISPLAY_PREFERENCES;
  const candidate = value as Partial<MpsDisplayPreferences>;
  return {
    horizonWeeks: isValidHorizon(candidate.horizonWeeks)
      ? candidate.horizonWeeks
      : DEFAULT_MPS_DISPLAY_PREFERENCES.horizonWeeks,
    dateBasis: isMpsDateBasis(candidate.dateBasis)
      ? candidate.dateBasis
      : DEFAULT_MPS_DISPLAY_PREFERENCES.dateBasis,
  };
}

/**
 * Loads the last-used MPS horizon/date-basis display preference from local storage.
 * Purely a local view preference: never triggers a QAD reload and is independent of any
 * particular workspace's data snapshot.
 */
export function loadMpsDisplayPreferences(): MpsDisplayPreferences {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return DEFAULT_MPS_DISPLAY_PREFERENCES;
    return normalize(JSON.parse(raw));
  } catch {
    return DEFAULT_MPS_DISPLAY_PREFERENCES;
  }
}

/** Persists the MPS horizon/date-basis display preference to local storage. */
export function saveMpsDisplayPreferences(preferences: MpsDisplayPreferences): void {
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(preferences));
}

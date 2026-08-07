import { describe, it, expect, beforeEach } from 'vitest';
import {
  DEFAULT_MPS_DISPLAY_PREFERENCES,
  loadMpsDisplayPreferences,
  saveMpsDisplayPreferences,
} from './mpsDisplayPreferences';

describe('mpsDisplayPreferences', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it('returns the default preference when nothing has been saved', () => {
    expect(loadMpsDisplayPreferences()).toEqual(DEFAULT_MPS_DISPLAY_PREFERENCES);
  });

  it('round-trips a saved horizon and date basis', () => {
    saveMpsDisplayPreferences({ horizonWeeks: 24, dateBasis: 'releaseDate' });
    expect(loadMpsDisplayPreferences()).toEqual({ horizonWeeks: 24, dateBasis: 'releaseDate' });
  });

  it('falls back to defaults when stored JSON is malformed', () => {
    window.localStorage.setItem('kst.mpsDisplayPreferences.v1', '{not json');
    expect(loadMpsDisplayPreferences()).toEqual(DEFAULT_MPS_DISPLAY_PREFERENCES);
  });

  it('falls back to defaults when the stored horizon is out of range', () => {
    window.localStorage.setItem(
      'kst.mpsDisplayPreferences.v1',
      JSON.stringify({ horizonWeeks: 999, dateBasis: 'dueDate' }),
    );
    expect(loadMpsDisplayPreferences().horizonWeeks).toBe(DEFAULT_MPS_DISPLAY_PREFERENCES.horizonWeeks);
  });

  it('falls back to defaults when the stored date basis is unrecognized', () => {
    window.localStorage.setItem(
      'kst.mpsDisplayPreferences.v1',
      JSON.stringify({ horizonWeeks: 12, dateBasis: 'shipDate' }),
    );
    expect(loadMpsDisplayPreferences().dateBasis).toBe(DEFAULT_MPS_DISPLAY_PREFERENCES.dateBasis);
  });
});

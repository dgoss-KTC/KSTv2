import { useState, useEffect, useCallback } from 'react';
import type { UserPreferencesDto } from '../api/client';
import { fetchPreferences, updatePreferences as updatePreferencesApi } from '../api/preferencesApi';

const DEFAULT_PREFERENCES: UserPreferencesDto = {
  theme: 'system',
  accentColor: 'blue',
  rowDensity: 'compact',
};

function isUserPreferencesDto(value: unknown): value is UserPreferencesDto {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<UserPreferencesDto>;
  return (
    typeof candidate.theme === 'string' &&
    typeof candidate.accentColor === 'string' &&
    typeof candidate.rowDensity === 'string'
  );
}

/**
 * Loads and persists local user preferences (theme, accent color, row density).
 * Defensive by design: any malformed/unexpected response (missing shape, network error,
 * or an unrelated JSON body from an unmatched route) is treated as "use local defaults"
 * rather than surfaced as a fatal error, since preferences are nonfatal, local-only state.
 */
export function usePreferences() {
  const [preferences, setPreferences] = useState<UserPreferencesDto>(DEFAULT_PREFERENCES);
  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>('dark');
  const [configurationWarning, setConfigurationWarning] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const result = await fetchPreferences();
      if (result && isUserPreferencesDto(result.preferences)) {
        setPreferences(result.preferences);
        setConfigurationWarning(result.configurationWarning ?? null);
      }
    } catch {
      // Nonfatal — keep local defaults.
    }
  }, []);

  useEffect(() => {
    // Wrap in setTimeout so setState is called from a callback, not the effect body directly.
    const id = setTimeout(() => void load(), 0);
    return () => clearTimeout(id);
  }, [load]);

  // Resolve the effective light/dark theme, tracking the OS preference live when theme is 'system'.
  useEffect(() => {
    if (preferences.theme !== 'system') {
      const resolved = preferences.theme === 'light' ? 'light' : 'dark';
      const id = setTimeout(() => setResolvedTheme(resolved), 0);
      return () => clearTimeout(id);
    }

    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      const id = setTimeout(() => setResolvedTheme('dark'), 0);
      return () => clearTimeout(id);
    }

    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const update = () => setResolvedTheme(media.matches ? 'dark' : 'light');
    const initialId = setTimeout(update, 0);
    media.addEventListener('change', update);
    return () => {
      clearTimeout(initialId);
      media.removeEventListener('change', update);
    };
  }, [preferences.theme]);

  const updatePreferences = useCallback(
    async (next: Partial<UserPreferencesDto>): Promise<void> => {
      const previous = preferences;
      const merged = { ...preferences, ...next };
      setPreferences(merged);
      try {
        const result = await updatePreferencesApi(merged);
        setPreferences(result.preferences);
      } catch (err) {
        setPreferences(previous);
        throw err;
      }
    },
    [preferences],
  );

  return {
    preferences,
    resolvedTheme,
    configurationWarning,
    updatePreferences,
  };
}

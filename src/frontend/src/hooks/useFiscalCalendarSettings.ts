import { useCallback, useState } from 'react';
import {
  loadFiscalCalendarSettings,
  saveFiscalCalendarSettings,
  validateFiscalYearException,
  type FiscalExceptionValidationError,
} from '../fiscal/fiscalCalendarSettings';
import type { FiscalCalendarSettings, FiscalYearException } from '../fiscal/types';

/**
 * Manages fiscal calendar settings (anchor + 53-week exceptions), persisted to local storage.
 * Fiscal semantics are frontend-only: no backend call is made to read or write this state.
 */
export function useFiscalCalendarSettings() {
  const [settings, setSettings] = useState<FiscalCalendarSettings>(() => loadFiscalCalendarSettings());

  const addException = useCallback(
    (candidate: FiscalYearException): FiscalExceptionValidationError[] => {
      const errors = validateFiscalYearException(settings.exceptions, candidate);
      if (errors.length > 0) return errors;

      const next: FiscalCalendarSettings = {
        ...settings,
        exceptions: [...settings.exceptions, candidate].sort((a, b) => a.fiscalYear - b.fiscalYear),
      };
      setSettings(next);
      saveFiscalCalendarSettings(next);
      return [];
    },
    [settings],
  );

  const removeException = useCallback(
    (fiscalYear: number) => {
      const next: FiscalCalendarSettings = {
        ...settings,
        exceptions: settings.exceptions.filter((e) => e.fiscalYear !== fiscalYear),
      };
      setSettings(next);
      saveFiscalCalendarSettings(next);
    },
    [settings],
  );

  return { settings, addException, removeException };
}

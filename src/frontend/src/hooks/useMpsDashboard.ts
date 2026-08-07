import { useCallback, useEffect, useState } from 'react';
import type { MpsDashboardResponseDto } from '../api/client';
import {
  fetchMpsDashboard,
  refreshMpsDashboard,
  toMpsApiError,
  type MpsApiError,
  type MpsDateBasis,
} from '../api/mpsApi';

export const DEFAULT_MPS_HORIZON_WEEKS = 12;
export const MIN_MPS_HORIZON_WEEKS = 1;
export const MAX_MPS_HORIZON_WEEKS = 72;

export interface MpsDashboardState {
  dashboard: MpsDashboardResponseDto | null;
  dateBasis: MpsDateBasis;
  horizonWeeks: number;
  isLoading: boolean;
  isRefreshing: boolean;
  error: MpsApiError | null;
}

/**
 * Loads and locally re-projects the MPS dashboard for one workspace. Changing dateBasis/horizonWeeks
 * re-calls GET (fast local re-projection on the backend, no QAD re-query); only refresh() forces QAD reload.
 */
export function useMpsDashboard(assignmentId: string) {
  const [dateBasis, setDateBasis] = useState<MpsDateBasis>('dueDate');
  const [horizonWeeks, setHorizonWeeks] = useState<number>(DEFAULT_MPS_HORIZON_WEEKS);
  const [dashboard, setDashboard] = useState<MpsDashboardResponseDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<MpsApiError | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const result = await fetchMpsDashboard(assignmentId, dateBasis, horizonWeeks);
      setDashboard(result);
      setError(null);
    } catch (err) {
      const mpsError = toMpsApiError(err);
      setError(mpsError ?? { type: 'unavailable', detail: 'Could not load MPS data. Try again.' });
    } finally {
      setIsLoading(false);
    }
  }, [assignmentId, dateBasis, horizonWeeks]);

  useEffect(() => {
    const id = setTimeout(() => void load(), 0);
    return () => clearTimeout(id);
  }, [load]);

  const refresh = useCallback(async () => {
    setIsRefreshing(true);
    try {
      const result = await refreshMpsDashboard(assignmentId, dateBasis, horizonWeeks);
      setDashboard(result);
      setError(null);
    } catch (err) {
      const mpsError = toMpsApiError(err);
      setError(mpsError ?? { type: 'unavailable', detail: 'Could not refresh MPS data. Try again.' });
    } finally {
      setIsRefreshing(false);
    }
  }, [assignmentId, dateBasis, horizonWeeks]);

  return {
    dashboard,
    dateBasis,
    horizonWeeks,
    isLoading,
    isRefreshing,
    error,
    setDateBasis,
    setHorizonWeeks,
    reload: load,
    refresh,
  };
}

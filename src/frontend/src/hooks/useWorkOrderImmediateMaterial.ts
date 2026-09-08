import { useCallback, useEffect, useState } from 'react';
import type { WorkOrderImmediateMaterialAnalysisResponseDto } from '../api/client';
import { fetchWorkOrderImmediateMaterial, toWorkOrdersApiError, type WorkOrdersApiError } from '../api/workOrdersApi';

export interface WorkOrderImmediateMaterialState {
  analysis: WorkOrderImmediateMaterialAnalysisResponseDto | null;
  isLoading: boolean;
  error: WorkOrdersApiError | null;
  retry: () => void;
}

/** Selected-panel Stage 9 request state. The backend remains the sole shortage-calculation and cache owner. */
export function useWorkOrderImmediateMaterial(
  assignmentId: string,
  snapshotId: string | null,
  woid: string | null,
  dateBasis: string,
  enabled: boolean,
): WorkOrderImmediateMaterialState {
  const [analysis, setAnalysis] = useState<WorkOrderImmediateMaterialAnalysisResponseDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<WorkOrdersApiError | null>(null);

  const load = useCallback(async () => {
    if (!enabled || snapshotId === null || woid === null) {
      setAnalysis(null);
      setError(null);
      setIsLoading(false);
      return;
    }
    setIsLoading(true);
    try {
      setAnalysis(await fetchWorkOrderImmediateMaterial(assignmentId, snapshotId, woid, dateBasis));
      setError(null);
    } catch (err) {
      setAnalysis(null);
      setError(toWorkOrdersApiError(err) ?? { type: 'error', detail: 'Could not load immediate material analysis. Try again.' });
    } finally {
      setIsLoading(false);
    }
  }, [assignmentId, snapshotId, woid, dateBasis, enabled]);

  useEffect(() => {
    const id = setTimeout(() => void load(), 0);
    return () => clearTimeout(id);
  }, [load]);

  return { analysis, isLoading, error, retry: load };
}

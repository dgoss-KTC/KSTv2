import { useCallback, useEffect, useState } from 'react';
import type { KittingSummaryDto, WorkOrderMaterialLineDto } from '../api/client';
import { fetchWorkOrderMaterialLines, toWorkOrdersApiError, type WorkOrdersApiError } from '../api/workOrdersApi';

export interface WorkOrderMaterialLinesState {
  lines: WorkOrderMaterialLineDto[] | null;
  kitting: KittingSummaryDto | null;
  isLoading: boolean;
  error: WorkOrdersApiError | null;
  retry: () => void;
}

/**
 * Loads lazily the material/kitting lines for one work order, only while `enabled` (its card is
 * expanded). Collapsing clears state without calling the backend; re-expanding re-fetches.
 */
export function useWorkOrderMaterialLines(
  assignmentId: string,
  snapshotId: string | null,
  woid: string,
  enabled: boolean,
): WorkOrderMaterialLinesState {
  const [lines, setLines] = useState<WorkOrderMaterialLineDto[] | null>(null);
  const [kitting, setKitting] = useState<KittingSummaryDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<WorkOrdersApiError | null>(null);

  const load = useCallback(async () => {
    if (!enabled || snapshotId === null) {
      setLines(null);
      setKitting(null);
      setError(null);
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    try {
      const result = await fetchWorkOrderMaterialLines(assignmentId, snapshotId, woid);
      setLines(result.lines);
      setKitting(result.kitting);
      setError(null);
    } catch (err) {
      setLines(null);
      setKitting(null);
      setError(toWorkOrdersApiError(err) ?? { type: 'error', detail: 'Could not load material lines. Try again.' });
    } finally {
      setIsLoading(false);
    }
  }, [assignmentId, snapshotId, woid, enabled]);

  useEffect(() => {
    const id = setTimeout(() => void load(), 0);
    return () => clearTimeout(id);
  }, [load]);

  return { lines, kitting, isLoading, error, retry: load };
}

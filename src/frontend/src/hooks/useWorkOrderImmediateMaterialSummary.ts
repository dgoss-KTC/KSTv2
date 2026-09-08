import { useCallback, useEffect, useState } from 'react';
import type { WorkOrderImmediateMaterialSummaryResponseDto } from '../api/client';
import { fetchWorkOrderImmediateMaterialSummary, toWorkOrdersApiError, type WorkOrdersApiError } from '../api/workOrdersApi';

export function useWorkOrderImmediateMaterialSummary(assignmentId: string, snapshotId: string | null, dateBasis: string) {
  const [summary, setSummary] = useState<WorkOrderImmediateMaterialSummaryResponseDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<WorkOrdersApiError | null>(null);
  const load = useCallback(async () => {
    if (!snapshotId) { setSummary(null); setError(null); setIsLoading(false); return; }
    setIsLoading(true);
    try { setSummary(await fetchWorkOrderImmediateMaterialSummary(assignmentId, snapshotId, null, dateBasis)); setError(null); }
    catch (err) { setSummary(null); setError(toWorkOrdersApiError(err) ?? { type: 'error', detail: 'Could not load shortage indicators.' }); }
    finally { setIsLoading(false); }
  }, [assignmentId, snapshotId, dateBasis]);
  useEffect(() => { const id = setTimeout(() => void load(), 0); return () => clearTimeout(id); }, [load]);
  return { summary, isLoading, error, retry: load };
}

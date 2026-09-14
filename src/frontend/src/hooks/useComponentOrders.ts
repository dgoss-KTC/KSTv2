import { useCallback, useEffect, useRef, useState } from 'react';
import { fetchComponentOrders, toComponentOrdersApiError, type ComponentOrdersApiError, type ComponentOrdersResponse } from '../api/componentOrdersApi';

interface UseComponentOrdersResult {
  data: ComponentOrdersResponse | null;
  isLoading: boolean;
  error: ComponentOrdersApiError | null;
  retry: () => void;
}

/**
 * Loads the active workspace's Component Orders for one MPS snapshot. Fetches when both the
 * assignment and a current snapshot id are present (no snapshot yet → neutral "load first" state,
 * never an error). A new snapshot id refetches automatically after an MPS refresh; in-flight
 * responses from superseded requests are discarded.
 */
export function useComponentOrders(assignmentId: string, snapshotId: string | null): UseComponentOrdersResult {
  const [data, setData] = useState<ComponentOrdersResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<ComponentOrdersApiError | null>(null);
  const [reloadToken, setReloadToken] = useState(0);
  const requestIdRef = useRef(0);

  useEffect(() => {
    if (!snapshotId) return;
    const requestId = ++requestIdRef.current;
    setIsLoading(true);
    setError(null);
    setData(null);
    fetchComponentOrders(assignmentId, snapshotId)
      .then((response) => {
        if (requestId !== requestIdRef.current) return;
        setData(response);
        setIsLoading(false);
      })
      .catch((err: unknown) => {
        if (requestId !== requestIdRef.current) return;
        setError(toComponentOrdersApiError(err) ?? { type: 'error', detail: String(err) });
        setIsLoading(false);
      });
  }, [assignmentId, snapshotId, reloadToken]);

  const retry = useCallback(() => setReloadToken((token) => token + 1), []);

  return { data, isLoading, error, retry };
}

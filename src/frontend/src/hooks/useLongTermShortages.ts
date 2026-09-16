import { startTransition, useEffect, useRef, useState } from 'react';
import {
  fetchLongTermShortages,
  toLongTermShortagesApiError,
  type LongTermShortagePopulationOptions,
  type LongTermShortagesApiError,
  type LongTermShortagesResponse,
} from '../api/longTermShortagesApi';

export function useLongTermShortages(
  assignmentId: string,
  snapshotId: string | null,
  populationOptions: LongTermShortagePopulationOptions,
) {
  const { includeManufacturedParts, includePhantoms } = populationOptions;
  const [data, setData] = useState<LongTermShortagesResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<LongTermShortagesApiError | null>(null);
  const [reloadToken, setReloadToken] = useState(0);
  const requestId = useRef(0);

  useEffect(() => {
    if (!snapshotId) return;
    const id = ++requestId.current;
    // Keep a superseded snapshot out of view while the next snapshot is loading.
    startTransition(() => {
      setData(null);
      setError(null);
      setIsLoading(true);
    });
    fetchLongTermShortages(assignmentId, snapshotId, { includeManufacturedParts, includePhantoms })
      .then((result) => {
        if (id === requestId.current) setData(result);
      })
      .catch((reason: unknown) => {
        if (id === requestId.current) setError(toLongTermShortagesApiError(reason));
      })
      .finally(() => {
        if (id === requestId.current) setIsLoading(false);
      });
  }, [assignmentId, includeManufacturedParts, includePhantoms, reloadToken, snapshotId]);

  return { data, isLoading, error, retry: () => setReloadToken((value) => value + 1) };
}

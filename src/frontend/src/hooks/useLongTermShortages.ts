import { useEffect, useRef, useState } from 'react';
import {
  fetchLongTermShortages,
  toLongTermShortagesApiError,
  type LongTermShortagePopulationOptions,
  type LongTermShortagesApiError,
  type LongTermShortagesScreen as LongTermShortagesResponse,
} from '../api/longTermShortagesApi';

interface LoadedResult { key: string; result: LongTermShortagesResponse; options: LongTermShortagePopulationOptions }

export function useLongTermShortages(
  assignmentId: string,
  snapshotId: string | null,
  populationOptions: LongTermShortagePopulationOptions,
) {
  const { includeManufacturedParts, includePhantoms, horizonWeeks } = populationOptions;
  const [loaded, setLoaded] = useState<LoadedResult | null>(null);
  const [pendingKey, setPendingKey] = useState<string | null>(null);
  const [error, setError] = useState<LongTermShortagesApiError | null>(null);
  const [reloadToken, setReloadToken] = useState(0);
  const requestId = useRef(0);
  const retriedKey = useRef<string | null>(null);
  const cache = useRef(new Map<string, LongTermShortagesResponse>());
  const inFlight = useRef(new Map<string, Promise<LongTermShortagesResponse>>());
  const identity = `${assignmentId}\u0000${snapshotId ?? ''}`;
  const key = `${identity}\u0000${includeManufacturedParts}\u0000${includePhantoms}\u0000${horizonWeeks}`;
  const visible = loaded?.key.startsWith(`${identity}\u0000`) ? loaded.result : null;

  useEffect(() => {
    if (!snapshotId) return;
    const id = ++requestId.current;
    const options = { includeManufacturedParts, includePhantoms, horizonWeeks, includeUnconfirmed: false, showAll: true };
    const cached = cache.current.get(key);
    if (cached && retriedKey.current !== key) {
      queueMicrotask(() => { if (id === requestId.current) { setLoaded({ key, result: cached, options }); setPendingKey(null); setError(null); } });
      return () => { if (requestId.current === id) requestId.current = id + 1; };
    }

    queueMicrotask(() => { if (id === requestId.current) { setPendingKey(key); setError(null); } });
    const bypassInFlight = retriedKey.current === key;
    retriedKey.current = null;
    let request = inFlight.current.get(key);
    if (!request || bypassInFlight) {
      // Show All and receipt mode are display choices over both returned projections.
      request = fetchLongTermShortages(assignmentId, snapshotId, { includeManufacturedParts, includePhantoms, includeUnconfirmed: false, horizonWeeks, showAll: true });
      inFlight.current.set(key, request);
      void request.finally(() => { if (inFlight.current.get(key) === request) inFlight.current.delete(key); }).catch(() => {});
    }
    void request.then((result) => {
      cache.current.set(key, result);
      if (id === requestId.current) setLoaded({ key, result, options });
    }).catch((reason: unknown) => {
      if (id === requestId.current) setError(toLongTermShortagesApiError(reason));
    }).finally(() => {
      if (id === requestId.current) setPendingKey(null);
    });
    return () => { if (requestId.current === id) requestId.current = id + 1; };
  }, [assignmentId, includeManufacturedParts, includePhantoms, horizonWeeks, key, reloadToken, snapshotId]);

  return {
    data: visible,
    loadedOptions: visible ? loaded!.options : populationOptions,
    isLoading: !visible && pendingKey === key,
    isRefreshing: !!visible && pendingKey === key,
    error,
    retry: () => { cache.current.delete(key); retriedKey.current = key; setReloadToken((value) => value + 1); },
  };
}

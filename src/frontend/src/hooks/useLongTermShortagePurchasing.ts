import { useEffect, useState } from 'react';
import { fetchLongTermShortagePurchasing, type LongTermShortagePopulationOptions, type LongTermShortagePurchasing } from '../api/longTermShortagesApi';

/** Lazy, selected-component detail. A new selection ignores old responses; successful detail is cached by workspace/snapshot/options. */
export function useLongTermShortagePurchasing(assignmentId: string, snapshotId: string | null,
  componentPart: string | null, options: LongTermShortagePopulationOptions) {
  const [state, setState] = useState<{ key: string; data: LongTermShortagePurchasing | null; error: boolean }>({ key: '', data: null, error: false });
  const [retry, setRetry] = useState(0);
  const [cache] = useState(() => new Map<string, LongTermShortagePurchasing>());
  const { includeManufacturedParts, includePhantoms, horizonWeeks } = options;
  const key = `${assignmentId}\u0000${snapshotId}\u0000${componentPart}\u0000${includeManufacturedParts}\u0000${includePhantoms}\u0000${horizonWeeks}\u0000${retry}`;
  useEffect(() => {
    if (!snapshotId || !componentPart) return;
    if (cache.has(key)) return;
    let active = true;
    fetchLongTermShortagePurchasing(assignmentId, snapshotId, componentPart,
      { includeManufacturedParts, includePhantoms, horizonWeeks, includeUnconfirmed: false, showAll: true })
      .then((data) => { if (active) { cache.set(key, data); setState({ key, data, error: false }); } })
      .catch(() => { if (active) setState({ key, data: null, error: true }); });
    return () => { active = false; };
  }, [assignmentId, snapshotId, componentPart, includeManufacturedParts, includePhantoms, horizonWeeks, key, cache]);
  return {
    data: cache.get(key) ?? (state.key === key ? state.data : null),
    isLoading: !!snapshotId && !!componentPart && state.key !== key && !cache.has(key),
    error: state.key === key && state.error,
    retry: () => setRetry((value) => value + 1),
  };
}

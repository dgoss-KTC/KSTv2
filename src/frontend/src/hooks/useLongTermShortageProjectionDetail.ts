import { useEffect, useState } from 'react';
import { fetchLongTermShortageProjectionDetail, type LongTermShortagePopulationOptions, type LongTermShortageProjectionDetail } from '../api/longTermShortagesApi';

/** Holds only the current selection. Late responses cannot hydrate a different snapshot or component. */
export function useLongTermShortageProjectionDetail(assignmentId: string, snapshotId: string | null,
  componentPart: string | null, options: LongTermShortagePopulationOptions) {
  const [state, setState] = useState<{ key: string; data: LongTermShortageProjectionDetail | null; error: boolean }>({ key: '', data: null, error: false });
  const [retry, setRetry] = useState(0);
  const { includeManufacturedParts, includePhantoms, horizonWeeks } = options;
  const key = `${assignmentId}\u0000${snapshotId}\u0000${componentPart}\u0000${includeManufacturedParts}\u0000${includePhantoms}\u0000${horizonWeeks}\u0000${retry}`;
  useEffect(() => {
    if (!snapshotId || !componentPart) return;
    let active = true;
    void fetchLongTermShortageProjectionDetail(assignmentId, snapshotId, componentPart,
      { includeManufacturedParts, includePhantoms, horizonWeeks, includeUnconfirmed: false, showAll: true })
      .then((data) => {
        if (data.snapshotId !== snapshotId || data.componentPart !== componentPart) throw new Error('Projection identity mismatch');
        if (active) setState({ key, data, error: false });
      }).catch(() => { if (active) setState({ key, data: null, error: true }); });
    return () => { active = false; };
  }, [assignmentId, snapshotId, componentPart, includeManufacturedParts, includePhantoms, horizonWeeks, key]);
  return {
    data: state.key === key ? state.data : null,
    isLoading: !!snapshotId && !!componentPart && state.key !== key,
    error: state.key === key && state.error,
    retry: () => setRetry(value => value + 1),
  };
}

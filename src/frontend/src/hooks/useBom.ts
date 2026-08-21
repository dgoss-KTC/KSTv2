import { useCallback, useEffect, useRef, useState } from 'react';
import type { BomResponseDto } from '../api/client';
import { fetchBom, toBomApiError, type BomApiError } from '../api/bomApi';

export interface BomState {
  bom: BomResponseDto | null;
  isLoading: boolean;
  error: BomApiError | null;
  /**
   * Explicit activation (the BOM-tab click). Loads only on the first activation for the current
   * identity; activating an already-loaded identity (tab revisit) is a no-op that keeps the data.
   */
  activate: () => void;
  /** Re-issues the request for the current identity (the Retry button). */
  retry: () => void;
}

/**
 * The frontend BOM identity: workspace + selected parent + current MPS snapshot generation.
 * `snapshotId` is the frontend freshness identity only — the accepted 8D.3 request carries just
 * workspace + parent, never the snapshot. Returning `null` means "no valid identity" (no parent
 * selected or no MPS snapshot loaded), in which case no request may be issued.
 */
function bomIdentity(assignmentId: string, parentPart: string | null, snapshotId: string | null): string | null {
  if (parentPart === null || snapshotId === null) return null;
  return [assignmentId, parentPart, snapshotId].join('\u0000');
}

interface BomData {
  /** The identity the stored result was loaded for. */
  identity: string;
  bom: BomResponseDto | null;
  error: BomApiError | null;
  phase: 'loading' | 'loaded' | 'error';
}

/**
 * Loads the scheduler-visible BOM lazily for the workspace's currently selected MPS parent part.
 *
 * Loading is command-driven: `activate()` (the BOM-tab click) is the only path that starts a
 * request. An identity change — a successful MPS refresh (new snapshot id), a parent change, or
 * a workspace change — can never by itself trigger a request (accepted Stage 8D.4 amendment:
 * no transient refresh-time request), and stored data is exposed only while its identity matches
 * the current one, so rows from a previous context can never render as if they belonged to the
 * new one. The next explicit BOM-tab activation after an identity change re-requests.
 *
 * A response or error whose request identity is no longer the current one is never committed
 * (accepted Stage 8D.4 amendment: obsolete responses must not populate a newer context).
 */
export function useBom(
  assignmentId: string,
  parentPart: string | null,
  snapshotId: string | null,
): BomState {
  const [data, setData] = useState<BomData | null>(null);

  const identity = bomIdentity(assignmentId, parentPart, snapshotId);

  // Tracks the current identity across async fetch resolution so an in-flight request from an
  // older identity can detect that it is obsolete.
  const currentIdentityRef = useRef(identity);
  useEffect(() => {
    currentIdentityRef.current = identity;
  }, [identity]);

  const startLoad = useCallback(
    (requestIdentity: string) => {
      // `requestIdentity` is non-null only when both parentPart and snapshotId are non-null.
      setData({ identity: requestIdentity, phase: 'loading', bom: null, error: null });
      void fetchBom(assignmentId, parentPart as string)
        .then((result) => {
          if (requestIdentity !== currentIdentityRef.current) return; // obsolete — ignore
          setData({ identity: requestIdentity, phase: 'loaded', bom: result, error: null });
        })
        .catch((err: unknown) => {
          if (requestIdentity !== currentIdentityRef.current) return; // obsolete — ignore
          setData({
            identity: requestIdentity,
            phase: 'error',
            bom: null,
            error: toBomApiError(err) ?? { type: 'error', detail: 'Could not load the BOM. Try again.' },
          });
        });
    },
    [assignmentId, parentPart],
  );

  const activate = useCallback(() => {
    if (identity === null) return;
    // First activation for this identity (or a re-activation after an error): load. A revisit of
    // an identity whose BOM is already loaded or loading keeps the current state — no request.
    if (data !== null && data.identity === identity && data.phase !== 'error') return;
    startLoad(identity);
  }, [identity, data, startLoad]);

  const retry = useCallback(() => {
    if (identity === null) return;
    startLoad(identity);
  }, [identity, startLoad]);

  // Identity-scoped view of the stored data: a stored result for a previous identity is never
  // exposed, so parent/workspace/snapshot transitions cannot display prior-context rows.
  const current = data !== null && data.identity === identity ? data : null;

  return {
    bom: current?.bom ?? null,
    isLoading: current !== null && current.phase === 'loading',
    error: current !== null && current.phase === 'error' ? current.error : null,
    activate,
    retry,
  };
}

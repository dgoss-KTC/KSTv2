import { useCallback, useEffect, useRef, useState } from 'react';
import type { ApprovedVendorDto } from '../api/client';
import {
  fetchApprovedVendors,
  toApprovedVendorsApiError,
  type ApprovedVendorsApiError,
} from '../api/approvedVendorsApi';

export interface ApprovedVendorsState {
  rows: ApprovedVendorDto[] | null;
  isLoading: boolean;
  error: ApprovedVendorsApiError | null;
  /** Called on first Approved Vendors section expansion; no-op if already loading/loaded for the current component. */
  activate: () => void;
  /** Re-issues the request for the current component (the AVL section's Retry button). */
  retry: () => void;
}

function approvedVendorsIdentity(assignmentId: string, componentPart: string | null): string | null {
  if (componentPart === null) return null;
  return [assignmentId, componentPart].join('\u0000');
}

interface ApprovedVendorsData {
  identity: string;
  rows: ApprovedVendorDto[] | null;
  error: ApprovedVendorsApiError | null;
  phase: 'loading' | 'loaded' | 'error';
}

/**
 * Loads Approved Vendors (AVL) for the Component Information modal's currently inspected
 * component, strictly command-driven: no request is issued until `activate()` is called (the AVL
 * section's first expansion) — unlike `useComponentDetail`, this hook never auto-loads on identity
 * change. A successful (including empty) or failed result is retained for the current component
 * identity; re-invoking `activate()` after a successful/in-flight load is a no-op, so
 * collapse/re-expand within the same component lifetime never refetches. `retry()` always
 * re-issues the request regardless of phase. A synchronous request-generation guard (mirroring
 * `useComponentDetail`'s identity guard) ensures a late response for a component that is no longer
 * being inspected can never be committed, even though the blocking Component Information modal
 * always fully unmounts before a different component can be inspected in the current UI.
 */
export function useApprovedVendors(assignmentId: string, componentPart: string | null): ApprovedVendorsState {
  const [data, setData] = useState<ApprovedVendorsData | null>(null);

  const identity = approvedVendorsIdentity(assignmentId, componentPart);

  const currentIdentityRef = useRef(identity);
  useEffect(() => {
    currentIdentityRef.current = identity;
  }, [identity]);

  const startLoad = useCallback(
    (requestIdentity: string) => {
      setData({ identity: requestIdentity, phase: 'loading', rows: null, error: null });
      void fetchApprovedVendors(assignmentId, componentPart as string)
        .then((result) => {
          if (requestIdentity !== currentIdentityRef.current) return; // obsolete — ignore
          setData({ identity: requestIdentity, phase: 'loaded', rows: result, error: null });
        })
        .catch((err: unknown) => {
          if (requestIdentity !== currentIdentityRef.current) return; // obsolete — ignore
          setData({
            identity: requestIdentity,
            phase: 'error',
            rows: null,
            error: toApprovedVendorsApiError(err) ?? {
              type: 'error',
              detail: 'Could not load approved vendors. Try again.',
            },
          });
        });
    },
    [assignmentId, componentPart],
  );

  const current = data !== null && data.identity === identity ? data : null;

  const activate = useCallback(() => {
    if (identity === null) return;
    if (current !== null && (current.phase === 'loading' || current.phase === 'loaded')) return;
    startLoad(identity);
  }, [identity, current, startLoad]);

  const retry = useCallback(() => {
    if (identity === null) return;
    startLoad(identity);
  }, [identity, startLoad]);

  return {
    rows: current?.rows ?? null,
    isLoading: current !== null && current.phase === 'loading',
    error: current !== null && current.phase === 'error' ? current.error : null,
    activate,
    retry,
  };
}

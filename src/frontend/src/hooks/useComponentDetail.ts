import { useCallback, useEffect, useRef, useState } from 'react';
import type { ComponentDetailResponseDto } from '../api/client';
import { fetchComponentDetail, toComponentDetailApiError, type ComponentDetailApiError } from '../api/componentDetailApi';

export interface ComponentDetailState {
  detail: ComponentDetailResponseDto | null;
  isLoading: boolean;
  error: ComponentDetailApiError | null;
  /** Re-issues the request for the current component (the modal's Retry button). */
  retry: () => void;
}

function componentDetailIdentity(assignmentId: string, componentPart: string | null): string | null {
  if (componentPart === null) return null;
  return [assignmentId, componentPart].join('\u0000');
}

interface ComponentDetailData {
  identity: string;
  detail: ComponentDetailResponseDto | null;
  error: ComponentDetailApiError | null;
  phase: 'loading' | 'loaded' | 'error';
}

/**
 * Loads Component Detail for the Component Information modal's currently inspected component
 * (Site + Component Part). Loading is identity-driven: opening the modal for a component (a
 * non-null `componentPart`) starts a request immediately, and closing it (`componentPart = null`)
 * or switching to a different component immediately invalidates the previous identity, so a late
 * response or error for a component that is no longer being inspected can never be committed
 * (mirrors the accepted Stage 8D.4 `useBom` obsolete-response guard).
 */
export function useComponentDetail(assignmentId: string, componentPart: string | null): ComponentDetailState {
  const [data, setData] = useState<ComponentDetailData | null>(null);

  const identity = componentDetailIdentity(assignmentId, componentPart);

  // Tracks the current identity across async fetch resolution so an in-flight request for a
  // closed or since-replaced component can detect that it is obsolete.
  const currentIdentityRef = useRef(identity);
  useEffect(() => {
    currentIdentityRef.current = identity;
  }, [identity]);

  const startLoad = useCallback(
    (requestIdentity: string) => {
      setData({ identity: requestIdentity, phase: 'loading', detail: null, error: null });
      void fetchComponentDetail(assignmentId, componentPart as string)
        .then((result) => {
          if (requestIdentity !== currentIdentityRef.current) return; // obsolete — ignore
          setData({ identity: requestIdentity, phase: 'loaded', detail: result, error: null });
        })
        .catch((err: unknown) => {
          if (requestIdentity !== currentIdentityRef.current) return; // obsolete — ignore
          setData({
            identity: requestIdentity,
            phase: 'error',
            detail: null,
            error: toComponentDetailApiError(err) ?? {
              type: 'error',
              detail: 'Could not load component information. Try again.',
            },
          });
        });
    },
    [assignmentId, componentPart],
  );

  // Opening the modal for a new component (or reopening after close) loads immediately; there is
  // no explicit activation step here since the modal itself is the lazy-load trigger. The
  // setTimeout defers the setState out of the effect body itself (matches usePartDetail).
  useEffect(() => {
    if (identity === null) return;
    const id = setTimeout(() => startLoad(identity), 0);
    return () => clearTimeout(id);
  }, [identity, startLoad]);

  const retry = useCallback(() => {
    if (identity === null) return;
    startLoad(identity);
  }, [identity, startLoad]);

  const current = data !== null && data.identity === identity ? data : null;

  return {
    detail: current?.detail ?? null,
    isLoading: current !== null && current.phase === 'loading',
    error: current !== null && current.phase === 'error' ? current.error : null,
    retry,
  };
}

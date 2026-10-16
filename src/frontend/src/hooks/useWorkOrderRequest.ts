import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import { toWorkOrdersApiError, type WorkOrdersApiError } from '../api/workOrdersApi';

/** Identity-masked, latest-attempt-only publication for the existing read-only WO APIs.
 * Transport cancellation is not exposed by these clients; obsolete replies are discarded. */
export function useWorkOrderRequest<T>(key: string, enabled: boolean, request: () => Promise<T>) {
  const owner = useRef({ key, alive: false, attempt: 0 });
  useLayoutEffect(() => {
    owner.current = { key, alive: true, attempt: owner.current.attempt + 1 };
    return () => { owner.current.alive = false; owner.current.attempt++; };
  }, [key]);
  const [state, setState] = useState<{ key: string; data: T | null; isLoading: boolean; error: WorkOrdersApiError | null }>();
  const load = useCallback(async () => {
    if (!enabled || !owner.current.alive || owner.current.key !== key) return;
    const attempt = ++owner.current.attempt;
    setState({ key, data: null, isLoading: true, error: null });
    try {
      const data = await request();
      if (owner.current.alive && owner.current.key === key && attempt === owner.current.attempt) setState({ key, data, isLoading: false, error: null });
    } catch (err) {
      if (owner.current.alive && owner.current.key === key && attempt === owner.current.attempt) setState({ key, data: null, isLoading: false, error: toWorkOrdersApiError(err) ?? { type: 'error', detail: 'Could not load work order information. Try again.' } });
    }
  }, [enabled, key, request]);
  useEffect(() => {
    const timer = setTimeout(() => void load(), 0);
    return () => { clearTimeout(timer); owner.current.attempt++; };
  }, [load]);
  const current = state?.key === key && enabled ? state : null;
  return { data: current?.data ?? null, isLoading: enabled && (current?.isLoading ?? true), error: current?.error ?? null, retry: load };
}

import { createContext, useContext, useEffect, useId, useRef } from 'react';

export interface EscapeStackEntry {
  id: string;
  collapse: () => void;
}

export interface EscapeStackApi {
  push: (entry: EscapeStackEntry) => void;
  remove: (id: string) => void;
  /** Collapses the most-recently-opened registered level, if any. Returns whether one was popped. */
  popTop: () => boolean;
}

// Lets a single ancestor Escape handler (MpsWorkspace) pop the most-recently-opened nested
// expansion first (e.g. a Work Order card's material lines, then a manufactured-row candidate
// branch) without knowing about every expandable level ahead of time.
export const EscapeStackContext = createContext<EscapeStackApi | null>(null);

/** Registers `collapse` on the nearest EscapeStackContext while `isOpen` is true. No-op without a provider. */
export function useEscapeLevel(isOpen: boolean, collapse: () => void): void {
  const api = useContext(EscapeStackContext);
  const id = useId();
  const collapseRef = useRef(collapse);
  useEffect(() => {
    collapseRef.current = collapse;
  });

  useEffect(() => {
    if (!isOpen || !api) return;
    const entry: EscapeStackEntry = { id, collapse: () => collapseRef.current() };
    api.push(entry);
    return () => api.remove(id);
  }, [isOpen, api, id]);
}

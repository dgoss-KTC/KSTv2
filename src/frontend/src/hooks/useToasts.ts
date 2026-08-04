import { useCallback, useRef, useState } from 'react';

export interface ToastMessage {
  id: number;
  kind: 'success' | 'error';
  text: string;
}

const AUTO_DISMISS_MS = 4000;

export function useToasts() {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);
  const nextId = useRef(0);

  const dismissToast = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const showToast = useCallback(
    (kind: ToastMessage['kind'], text: string) => {
      const id = nextId.current++;
      setToasts((prev) => [...prev, { id, kind, text }]);
      setTimeout(() => dismissToast(id), AUTO_DISMISS_MS);
    },
    [dismissToast],
  );

  return { toasts, showToast, dismissToast };
}

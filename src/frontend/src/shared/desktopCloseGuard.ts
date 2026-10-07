/**
 * Native desktop window-close protection for unsaved work.
 *
 * A browser `beforeunload` handler does not reliably run when the Tauri window is closed, so the
 * Rust host keeps a close guard: the webview reports whether unsaved work exists, the host keeps the
 * window open on a close request and emits `kst-close-requested`, the webview confirms with its own
 * dialog and answers with `confirm_window_close`. Outside Tauri every function is a no-op.
 * Uses application commands and core events only — no additional Tauri permission is required.
 */
const CLOSE_REQUESTED_EVENT = 'kst-close-requested';

function isDesktop(): boolean {
  return typeof window !== 'undefined'
    && Boolean((window as unknown as { __TAURI_INTERNALS__?: { transformCallback?: unknown } }).__TAURI_INTERNALS__?.transformCallback);
}

/** Tells the desktop host whether closing the window would discard unsaved work. */
export async function reportCloseGuard(active: boolean): Promise<void> {
  if (!isDesktop()) return;
  try {
    const { invoke } = await import('@tauri-apps/api/core');
    await invoke('set_close_guard', { active });
  } catch {
    // The guard is protective only; a failed report must not break the workspace.
  }
}

/** Asks the host to close the window now (the user already confirmed discarding unsaved work). */
export async function confirmDesktopClose(): Promise<void> {
  if (!isDesktop()) return;
  const { invoke } = await import('@tauri-apps/api/core');
  await invoke('confirm_window_close');
}

/** Subscribes to native close requests that were held back because unsaved work exists. */
export function onDesktopCloseRequested(handler: () => void): () => void {
  if (!isDesktop()) return () => {};
  let disposed = false;
  let unlisten: (() => void) | null = null;
  void import('@tauri-apps/api/event').then(({ listen }) =>
    listen(CLOSE_REQUESTED_EVENT, () => handler()).then((fn) => {
      if (disposed) fn(); else unlisten = fn;
    }));
  return () => { disposed = true; unlisten?.(); };
}

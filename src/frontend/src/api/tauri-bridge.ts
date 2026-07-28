/**
 * Bridge between the frontend and the Tauri host.
 * Provides the backend base URL set by the Rust sidecar manager.
 * Falls back to a development URL when running outside Tauri (e.g., vitest).
 */

declare global {
  interface Window {
    __KST_BACKEND_URL__?: string;
  }
}

/** Returns the backend API base URL provided by the Tauri host. */
export function getBackendBaseUrl(): string {
  // Injected by the Tauri Rust layer via a Tauri command or window.__KST_BACKEND_URL__
  if (window.__KST_BACKEND_URL__) {
    return window.__KST_BACKEND_URL__;
  }
  // In development, fall back to the dev default (no port assigned yet)
  return import.meta.env.VITE_BACKEND_URL ?? 'http://127.0.0.1:15402';
}

/** Returns true when the app is running inside a Tauri window. */
export function isRunningInTauri(): boolean {
  return typeof window !== 'undefined' && '__TAURI_INTERNALS__' in window;
}

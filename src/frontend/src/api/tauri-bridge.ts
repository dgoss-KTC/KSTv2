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

const FALLBACK_BACKEND_URL =
  import.meta.env.VITE_BACKEND_URL ?? 'http://127.0.0.1:15402';

/** Returns the backend API base URL provided by the Tauri host. */
export function getBackendBaseUrl(): string {
  // Injected by the Tauri Rust layer via a Tauri command or window.__KST_BACKEND_URL__
  if (window.__KST_BACKEND_URL__) {
    return window.__KST_BACKEND_URL__;
  }
  // In development, fall back to the dev default (no port assigned yet)
  return FALLBACK_BACKEND_URL;
}

/** Returns true when the app is running inside a Tauri window. */
export function isRunningInTauri(): boolean {
  if (typeof window === 'undefined') {
    return false;
  }

  const g = globalThis as typeof globalThis & {
    isTauri?: boolean;
  };

  return (
    '__TAURI_INTERNALS__' in window ||
    g.isTauri === true ||
    navigator.userAgent.includes('Tauri')
  );
}

/**
 * Attempts to resolve and cache backend URL from the Tauri host.
 * Falls back to the last known URL, then to the static dev default.
 */
export async function resolveBackendBaseUrl(): Promise<string> {
  const cached = window.__KST_BACKEND_URL__;

  try {
    const { invoke } = await import('@tauri-apps/api/core');
    const baseUrl = await invoke<string | null>('get_backend_url');
    if (baseUrl && typeof baseUrl === 'string') {
      window.__KST_BACKEND_URL__ = baseUrl;
      return baseUrl;
    }
  } catch {
    // Ignore and continue to cache/fallback resolution.
  }

  return cached ?? FALLBACK_BACKEND_URL;
}

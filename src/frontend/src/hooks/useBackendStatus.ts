import { useState, useEffect, useCallback } from 'react';
import { ApiClient, ApiError, type SystemStatusResponse } from '../api/client';
import {
  getBackendBaseUrl,
  resolveBackendBaseUrl,
} from '../api/tauri-bridge';

export type ConnectionState =
  | 'starting'
  | 'waiting'
  | 'connected'
  | 'unavailable'
  | 'api_error';

export interface BackendState {
  connectionState: ConnectionState;
  status: SystemStatusResponse | null;
  errorMessage: string | null;
  lastUpdated: Date | null;
}

interface BackendUnavailablePayload {
  reason?: string;
  pid?: number;
  expected?: boolean;
  code?: number;
  signal?: number;
}

export function useBackendStatus(pollIntervalMs = 0) {
  const [state, setState] = useState<BackendState>({
    connectionState: 'starting',
    status: null,
    errorMessage: null,
    lastUpdated: null,
  });

  const fetchStatus = useCallback(async () => {
    let backendBaseUrl = getBackendBaseUrl();

    try {
      backendBaseUrl = await resolveBackendBaseUrl();
      // setState is deferred past the first await, avoiding a synchronous setState in an effect.
      setState((prev) => ({
        ...prev,
        connectionState: prev.connectionState === 'starting' ? 'waiting' : prev.connectionState,
      }));
      const client = new ApiClient(backendBaseUrl);
      const status = await client.getSystemStatus();
      setState({
        connectionState: 'connected',
        status,
        errorMessage: null,
        lastUpdated: new Date(),
      });
    } catch (err) {
      if (err instanceof ApiError) {
        setState({
          connectionState: 'api_error',
          status: null,
          errorMessage: `API error ${err.status}: ${err.message}`,
          lastUpdated: new Date(),
        });
      } else if (err instanceof TypeError) {
        // Network error (backend unreachable)
        setState({
          connectionState: 'unavailable',
          status: null,
          errorMessage: `Backend is unavailable at ${backendBaseUrl}. It may still be starting.`,
          lastUpdated: new Date(),
        });
      } else {
        setState({
          connectionState: 'unavailable',
          status: null,
          errorMessage: String(err),
          lastUpdated: new Date(),
        });
      }
    }
  }, []);

  useEffect(() => {
    // Wrap in setTimeout so setState is called from a callback, not the effect body directly.
    const initialId = setTimeout(() => void fetchStatus(), 0);
    if (pollIntervalMs > 0) {
      const pollId = setInterval(() => void fetchStatus(), pollIntervalMs);
      return () => { clearTimeout(initialId); clearInterval(pollId); };
    }
    return () => clearTimeout(initialId);
  }, [fetchStatus, pollIntervalMs]);

  // While Tauri is launching the sidecar, keep probing for the backend URL.
  useEffect(() => {
    if (state.connectionState === 'connected') {
      return;
    }

    const id = setInterval(() => void fetchStatus(), 1500);
    return () => clearInterval(id);
  }, [fetchStatus, state.connectionState]);

  // Listen for Tauri backend lifecycle events.
  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    const internals = (window as Window & {
      __TAURI_INTERNALS__?: {
        transformCallback?: unknown;
      };
    }).__TAURI_INTERNALS__;

    if (!internals?.transformCallback) {
      return;
    }

    const unlisteners: Array<() => void> = [];

    void import('@tauri-apps/api/event').then(({ listen }) => {
      void listen<{ baseUrl: string }>('backend-ready', (event) => {
        window.__KST_BACKEND_URL__ = event.payload.baseUrl;
        void fetchStatus();
      }).then((fn) => {
        unlisteners.push(fn);
      });

      const handleUnavailable = (payload: BackendUnavailablePayload) => {
        window.__KST_BACKEND_URL__ = undefined;
        const reason = payload.reason ?? 'Backend became unavailable.';
        setState({
          connectionState: 'unavailable',
          status: null,
          errorMessage: reason,
          lastUpdated: new Date(),
        });
      };

      void listen<BackendUnavailablePayload>('backend-unavailable', (event) => {
        handleUnavailable(event.payload);
      }).then((fn) => {
        unlisteners.push(fn);
      });

      void listen<BackendUnavailablePayload>('backend-terminated', (event) => {
        handleUnavailable(event.payload);
      }).then((fn) => {
        unlisteners.push(fn);
      });
    }).catch(() => {
      // Event API is unavailable outside Tauri; polling still handles recovery.
    });

    return () => {
      for (const unlisten of unlisteners) {
        unlisten();
      }
    };
  }, [fetchStatus]);

  return { ...state, refresh: fetchStatus };
}

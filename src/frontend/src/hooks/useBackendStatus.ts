import { useState, useEffect, useCallback } from 'react';
import { ApiClient, ApiError, type SystemStatusResponse } from '../api/client';
import { getBackendBaseUrl, isRunningInTauri } from '../api/tauri-bridge';

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

export function useBackendStatus(pollIntervalMs = 0) {
  const [state, setState] = useState<BackendState>({
    connectionState: 'starting',
    status: null,
    errorMessage: null,
    lastUpdated: null,
  });

  const fetchStatus = useCallback(async () => {
    setState((prev) => ({
      ...prev,
      connectionState: prev.connectionState === 'starting' ? 'waiting' : prev.connectionState,
    }));

    try {
      const client = new ApiClient(getBackendBaseUrl());
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
          errorMessage: 'Backend is unavailable. It may still be starting.',
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
    void fetchStatus();
    if (pollIntervalMs > 0) {
      const id = setInterval(() => void fetchStatus(), pollIntervalMs);
      return () => clearInterval(id);
    }
  }, [fetchStatus, pollIntervalMs]);

  // Listen for the Tauri backend-ready event so the frontend auto-recovers
  // when the sidecar finishes starting without requiring a manual retry.
  useEffect(() => {
    if (!isRunningInTauri()) return;

    let unlisten: (() => void) | undefined;

    void import('@tauri-apps/api/event').then(({ listen }) => {
      void listen<{ baseUrl: string }>('backend-ready', (event) => {
        window.__KST_BACKEND_URL__ = event.payload.baseUrl;
        void fetchStatus();
      }).then((fn) => {
        unlisten = fn;
      });
    });

    return () => {
      unlisten?.();
    };
  }, [fetchStatus]);

  return { ...state, refresh: fetchStatus };
}

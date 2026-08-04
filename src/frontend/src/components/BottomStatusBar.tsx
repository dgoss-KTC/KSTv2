import type { SystemStatusResponse } from '../api/client';
import type { ConnectionState } from '../hooks/useBackendStatus';
import './BottomStatusBar.css';

interface BottomStatusBarProps {
  connectionState: ConnectionState;
  status: SystemStatusResponse | null;
  onRefresh: () => void;
  isRefreshing: boolean;
}

const backendLabel: Record<ConnectionState, string> = {
  starting: 'Starting\u2026',
  waiting: 'Starting\u2026',
  connected: 'Connected',
  unavailable: 'Unavailable',
  api_error: 'Error',
};

function humanize(value: string): string {
  const spaced = value.replace(/([a-z])([A-Z])/g, '$1 $2');
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}

function formatTimestamp(value: string | null | undefined): string {
  if (!value) return 'Never';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Never';
  return date.toLocaleString();
}

export function BottomStatusBar({
  connectionState,
  status,
  onRefresh,
  isRefreshing,
}: BottomStatusBarProps) {
  return (
    <div className="bottom-bar" role="contentinfo">
      <button
        type="button"
        className="bottom-bar__refresh"
        onClick={onRefresh}
        disabled={isRefreshing}
      >
        {isRefreshing ? 'Refreshing\u2026' : 'Refresh'}
      </button>

      <div className="bottom-bar__right">
        <span className="bottom-bar__item">
          Backend: <strong>{backendLabel[connectionState]}</strong>
        </span>
        <span className="bottom-bar__item">
          Snapshot: <strong>{status ? humanize(status.snapshot.status) : 'Unknown'}</strong>
        </span>
        <span className="bottom-bar__item">
          Last successful refresh: <strong>{formatTimestamp(status?.lastSuccessfulRefreshAt)}</strong>
        </span>
      </div>
    </div>
  );
}

import type { SystemStatusResponse } from '../api/client';
import type { ConnectionState } from '../hooks/useBackendStatus';
import './StatusDashboard.css';

interface StatusDashboardProps {
  connectionState: ConnectionState;
  status: SystemStatusResponse | null;
  errorMessage: string | null;
  lastUpdated: Date | null;
  onRetry: () => void;
  onRefresh: () => void;
}

export function StatusDashboard({
  connectionState,
  status,
  errorMessage,
  lastUpdated,
  onRetry,
  onRefresh,
}: StatusDashboardProps) {
  const connectionLabel: Record<ConnectionState, string> = {
    starting: 'Starting backend…',
    waiting: 'Waiting for readiness…',
    connected: 'Connected',
    unavailable: 'Backend unavailable',
    api_error: 'API error',
  };

  const connectionClass: Record<ConnectionState, string> = {
    starting: 'status-starting',
    waiting: 'status-waiting',
    connected: 'status-connected',
    unavailable: 'status-error',
    api_error: 'status-error',
  };

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <h1 className="dashboard-title">
          {status?.applicationName ?? 'Keytronic Scheduler\u2019s Toolbox'}
        </h1>
        <span className="dashboard-version">
          v{status?.applicationVersion ?? '—'}
        </span>
      </header>

      <section className="status-section">
        <h2>Connection Status</h2>
        <div className={`status-badge ${connectionClass[connectionState]}`}>
          {connectionLabel[connectionState]}
        </div>
        {errorMessage && (
          <p className="error-message" role="alert">
            {errorMessage}
          </p>
        )}
        {lastUpdated && (
          <p className="last-updated">
            Last updated: {lastUpdated.toLocaleTimeString()}
          </p>
        )}
      </section>

      {status && (
        <section className="status-section">
          <h2>Backend Information</h2>
          <dl className="info-grid">
            <dt>Framework</dt>
            <dd>{status.backendFramework}</dd>

            <dt>Instance ID</dt>
            <dd className="mono">{status.backendInstanceId}</dd>

            <dt>Started At</dt>
            <dd>{new Date(status.startedAt).toLocaleString()}</dd>

            <dt>Current Backend Time</dt>
            <dd>{new Date(status.currentTime).toLocaleString()}</dd>

            <dt>Snapshot</dt>
            <dd>
              {status.snapshot.available
                ? `Loaded (${status.snapshot.snapshotId})`
                : `Not loaded (${status.snapshot.status})`}
            </dd>
          </dl>
        </section>
      )}

      {status && (
        <section className="status-section">
          <h2>Data Sources</h2>
          <ul className="data-sources">
            {status.dataSources.map((ds) => (
              <li key={ds.name} className="data-source-item">
                <span className="data-source-name">{ds.name}</span>
                <span className={`data-source-status ds-${ds.status}`}>
                  {ds.status}
                </span>
              </li>
            ))}
          </ul>
        </section>
      )}

      <section className="actions">
        {(connectionState === 'unavailable' || connectionState === 'api_error') && (
          <button className="btn btn-primary" onClick={onRetry}>
            Retry Connection
          </button>
        )}
        {connectionState === 'connected' && (
          <button className="btn btn-secondary" onClick={onRefresh}>
            Refresh Status
          </button>
        )}
      </section>
    </div>
  );
}

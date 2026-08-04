import type { ConnectionState } from '../hooks/useBackendStatus';
import './TopApplicationBar.css';

interface TopApplicationBarProps {
  version: string;
  connectionState: ConnectionState;
  configurationWarning: string | null;
}

const connectionLabel: Record<ConnectionState, string> = {
  starting: 'Starting\u2026',
  waiting: 'Starting\u2026',
  connected: 'Backend connected',
  unavailable: 'Backend unavailable',
  api_error: 'Backend error',
};

const connectionClass: Record<ConnectionState, string> = {
  starting: 'top-bar__status--starting',
  waiting: 'top-bar__status--starting',
  connected: 'top-bar__status--connected',
  unavailable: 'top-bar__status--error',
  api_error: 'top-bar__status--error',
};

export function TopApplicationBar({
  version,
  connectionState,
  configurationWarning,
}: TopApplicationBarProps) {
  const statusClass = connectionClass[connectionState];
  const label = configurationWarning ? 'Configuration warning' : connectionLabel[connectionState];
  const dotClass = configurationWarning
    ? 'top-bar__dot top-bar__dot--warning'
    : `top-bar__dot ${statusClass.replace('top-bar__status--', 'top-bar__dot--')}`;

  return (
    <div className="top-bar" role="banner">
      <div className="top-bar__left">
        <div className="top-bar__kmark" aria-hidden="true">K</div>
        <div className="top-bar__title-group">
          <span className="top-bar__app-name">KST</span>
          <span className="top-bar__app-subtitle">Keytronic Scheduler&apos;s Toolbox</span>
        </div>
        <span className="top-bar__version">v{version}</span>
      </div>

      <div className="top-bar__right">
        <div className="top-bar__status" title={configurationWarning ?? undefined}>
          <span className={dotClass} aria-hidden="true" />
          <span className="top-bar__status-label">{label}</span>
        </div>
      </div>
    </div>
  );
}

import type { ConnectionState } from '../hooks/useBackendStatus';
import './BottomStatusBar.css';

interface BottomStatusBarProps {
  connectionState: ConnectionState;
  configurationWarning: string | null;
}

const backendLabel: Record<ConnectionState, string> = {
  starting: 'Starting\u2026',
  waiting: 'Starting\u2026',
  connected: 'Connected',
  unavailable: 'Unavailable',
  api_error: 'Error',
};

const dotClass: Record<ConnectionState, string> = {
  starting: 'bottom-bar__dot--starting',
  waiting: 'bottom-bar__dot--starting',
  connected: 'bottom-bar__dot--connected',
  unavailable: 'bottom-bar__dot--error',
  api_error: 'bottom-bar__dot--error',
};

export function BottomStatusBar({ connectionState, configurationWarning }: BottomStatusBarProps) {
  return (
    <div className="bottom-bar" role="contentinfo">
      <div className="bottom-bar__right">
        <span className="bottom-bar__item" title={configurationWarning ?? undefined}>
          <span
            className={`bottom-bar__dot ${configurationWarning ? 'bottom-bar__dot--warning' : dotClass[connectionState]}`}
            aria-hidden="true"
          />
          Backend: <strong>{backendLabel[connectionState]}</strong>
        </span>
        {configurationWarning && <span className="bottom-bar__warning">Configuration warning</span>}
      </div>
    </div>
  );
}

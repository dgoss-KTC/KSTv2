import './TopApplicationBar.css';

interface TopApplicationBarProps {
  version: string;
}

export function TopApplicationBar({ version }: TopApplicationBarProps) {
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
    </div>
  );
}

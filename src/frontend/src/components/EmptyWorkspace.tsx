import './EmptyWorkspace.css';

export function EmptyWorkspace() {
  return (
    <div className="empty-workspace" aria-label="No workspaces configured">
      <div className="empty-workspace__kmark" aria-hidden="true">K</div>
      <p className="empty-workspace__title">Keytronic Scheduler&apos;s Toolbox</p>
      <p className="empty-workspace__hint">Use + to add a workspace</p>
    </div>
  );
}

import type { WorkspaceAssignmentDto } from '../api/client';
import './WorkspaceTabBar.css';

interface WorkspaceTabBarProps {
  workspaces: WorkspaceAssignmentDto[];
  activeId: string | null;
  onSelect: (id: string) => void;
  onAdd: () => void;
}

export function WorkspaceTabBar({ workspaces, activeId, onSelect, onAdd }: WorkspaceTabBarProps) {
  return (
    <div className="tab-bar" role="tablist" aria-label="Workspaces">
      {workspaces.map((w) => (
        <button
          key={w.assignmentId}
          role="tab"
          aria-selected={w.assignmentId === activeId}
          className={`tab-bar__tab${w.assignmentId === activeId ? ' tab-bar__tab--active' : ''}`}
          onClick={() => onSelect(w.assignmentId)}
        >
          {w.displayName ?? w.site}
        </button>
      ))}

      <button
        className="tab-bar__add"
        onClick={onAdd}
        title="Add workspace"
        aria-label="Add workspace"
      >
        +
      </button>

      <div className="tab-bar__spacer" aria-hidden="true" />
    </div>
  );
}

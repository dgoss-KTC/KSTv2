import type { WorkspaceAssignmentDto } from '../api/client';
import './WorkspacePlaceholder.css';

interface WorkspacePlaceholderProps {
  workspace: WorkspaceAssignmentDto;
}

export function WorkspacePlaceholder({ workspace }: WorkspacePlaceholderProps) {
  return (
    <div className="workspace-placeholder">
      <div className="workspace-placeholder__header">
        <h2 className="workspace-placeholder__name">{workspace.displayName ?? workspace.site}</h2>
        <span className="workspace-placeholder__badge">Workspace configured</span>
      </div>

      <dl className="workspace-placeholder__details">
        <dt>Site</dt>
        <dd>{workspace.site}</dd>

        {workspace.customerNumber && (
          <>
            <dt>Customer number</dt>
            <dd className="workspace-placeholder__mono">{workspace.customerNumber}</dd>
          </>
        )}

        {workspace.productLineFrom && (
          <>
            <dt>Product line</dt>
            <dd className="workspace-placeholder__mono">
              {workspace.productLineTo && workspace.productLineTo !== workspace.productLineFrom
                ? `${workspace.productLineFrom}\u2013${workspace.productLineTo}`
                : workspace.productLineFrom}
            </dd>
          </>
        )}
      </dl>

      <p className="workspace-placeholder__notice">Schedule data has not been loaded.</p>
    </div>
  );
}

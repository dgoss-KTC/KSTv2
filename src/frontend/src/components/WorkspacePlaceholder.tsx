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

        {workspace.parentParts && workspace.parentParts.length > 0 && (
          <>
            <dt>Parent parts</dt>
            <dd className="workspace-placeholder__mono">
              {workspace.parentParts.length === 1
                ? workspace.parentParts[0]
                : `${workspace.parentParts.length} parts`}
            </dd>
          </>
        )}
      </dl>

      <p className="workspace-placeholder__notice">Schedule data has not been loaded.</p>
    </div>
  );
}

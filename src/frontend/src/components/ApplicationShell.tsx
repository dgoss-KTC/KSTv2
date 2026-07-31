import { useState, useEffect } from 'react';
import { useBackendStatus } from '../hooks/useBackendStatus';
import { useWorkspaces } from '../hooks/useWorkspaces';
import { TopApplicationBar } from './TopApplicationBar';
import { WorkspaceTabBar } from './WorkspaceTabBar';
import { EmptyWorkspace } from './EmptyWorkspace';
import { AddWorkspaceDialog } from './AddWorkspaceDialog';
import { WorkspacePlaceholder } from './WorkspacePlaceholder';
import './ApplicationShell.css';

export function ApplicationShell({ appVersion }: { appVersion?: string }) {
  const [theme, setTheme] = useState<'dark' | 'light'>('dark');
  const [showAddDialog, setShowAddDialog] = useState(false);

  const { connectionState, status } = useBackendStatus();
  const { state: workspacesState, load, addWorkspace, selectWorkspace } = useWorkspaces();

  useEffect(() => {
    if (connectionState === 'connected') {
      load();
    }
  }, [connectionState, load]);

  const handleToggleTheme = () => setTheme((t) => (t === 'dark' ? 'light' : 'dark'));

  const handleAddWorkspace = async (fields: Parameters<typeof addWorkspace>[0]) => {
    await addWorkspace(fields);
    setShowAddDialog(false);
  };

  const activeWorkspace =
    workspacesState.activeId != null
      ? workspacesState.workspaces.find((w) => w.assignmentId === workspacesState.activeId)
      : null;

  const version = appVersion ?? status?.applicationVersion ?? '—';

  return (
    <div className="shell" data-theme={theme}>
      <TopApplicationBar
        version={version}
        connectionState={connectionState}
        configurationWarning={workspacesState.configurationWarning}
        theme={theme}
        onToggleTheme={handleToggleTheme}
      />

      <WorkspaceTabBar
        workspaces={workspacesState.workspaces}
        activeId={workspacesState.activeId}
        onSelect={selectWorkspace}
        onAdd={() => setShowAddDialog(true)}
      />

      <main className="shell__content">
        {workspacesState.workspaces.length === 0 ? (
          <EmptyWorkspace />
        ) : activeWorkspace ? (
          <WorkspacePlaceholder workspace={activeWorkspace} />
        ) : null}
      </main>

      {showAddDialog && (
        <AddWorkspaceDialog
          onSave={handleAddWorkspace}
          onClose={() => setShowAddDialog(false)}
        />
      )}
    </div>
  );
}

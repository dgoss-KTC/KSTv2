import { useState, useEffect } from 'react';
import { useBackendStatus } from '../hooks/useBackendStatus';
import { useWorkspaces } from '../hooks/useWorkspaces';
import { usePreferences } from '../hooks/usePreferences';
import { useToasts } from '../hooks/useToasts';
import type { WorkspaceAssignmentDto, UserPreferencesDto } from '../api/client';
import { TopApplicationBar } from './TopApplicationBar';
import { WorkspaceTabBar } from './WorkspaceTabBar';
import { EmptyWorkspace } from './EmptyWorkspace';
import { AddWorkspaceDialog } from './AddWorkspaceDialog';
import { ManageWorkspacesDialog } from './ManageWorkspacesDialog';
import { ConfirmDialog } from './ConfirmDialog';
import { ToastStack } from './ToastStack';
import { MpsWorkspace } from './MpsWorkspace';
import { GeneralWorkspace } from './GeneralWorkspace';
import { BottomStatusBar } from './BottomStatusBar';
import './ApplicationShell.css';

interface ConfirmState {
  title: string;
  body: string;
  confirmLabel: string;
  destructive: boolean;
  onConfirm: () => Promise<void>;
}

export function ApplicationShell({ appVersion }: { appVersion?: string }) {
  const [isGeneralActive, setIsGeneralActive] = useState(false);
  const [showAddDialog, setShowAddDialog] = useState(false);
  const [editingWorkspace, setEditingWorkspace] = useState<WorkspaceAssignmentDto | null>(null);
  const [showManageDialog, setShowManageDialog] = useState(false);
  const [confirmState, setConfirmState] = useState<ConfirmState | null>(null);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const { connectionState, status, triggerRefresh } = useBackendStatus();
  const { preferences, resolvedTheme, updatePreferences } = usePreferences();
  const {
    state: workspacesState,
    activeWorkspaces,
    archivedWorkspaces,
    load,
    addWorkspace,
    editWorkspace,
    archiveWorkspace,
    restoreWorkspace,
    deleteWorkspace,
    resetWorkspaces,
    reorderWorkspaces,
    selectWorkspace,
  } = useWorkspaces();
  const { toasts, showToast, dismissToast } = useToasts();

  useEffect(() => {
    if (connectionState === 'connected') {
      load();
    }
  }, [connectionState, load]);

  const handleSelectWorkspace = (id: string) => {
    setIsGeneralActive(false);
    selectWorkspace(id);
  };

  const handleSelectGeneral = () => setIsGeneralActive(true);

  const handleAddWorkspace = async (fields: Parameters<typeof addWorkspace>[0]) => {
    await addWorkspace(fields);
    setShowAddDialog(false);
  };

  const handleEditSave = async (fields: Parameters<typeof addWorkspace>[0]) => {
    if (!editingWorkspace) return;
    await editWorkspace(editingWorkspace.assignmentId, fields);
    setEditingWorkspace(null);
    showToast('success', 'Workspace updated');
  };

  const handleArchiveRequest = (ws: WorkspaceAssignmentDto) => {
    setConfirmState({
      title: 'Archive workspace?',
      body: 'This workspace will be removed from the tab bar but can be restored later.',
      confirmLabel: 'Archive',
      destructive: false,
      onConfirm: async () => {
        try {
          await archiveWorkspace(ws.assignmentId);
          showToast('success', 'Workspace archived');
        } catch {
          showToast('error', 'Could not archive the workspace. Try again.');
        } finally {
          setConfirmState(null);
        }
      },
    });
  };

  const handleDeleteRequest = (ws: WorkspaceAssignmentDto) => {
    setConfirmState({
      title: 'Delete workspace permanently?',
      body: 'This removes the saved workspace configuration and cannot be undone.',
      confirmLabel: 'Delete Permanently',
      destructive: true,
      onConfirm: async () => {
        try {
          await deleteWorkspace(ws.assignmentId);
          showToast('success', 'Workspace deleted');
        } catch {
          showToast('error', 'Could not delete the workspace. Try again.');
        } finally {
          setConfirmState(null);
        }
      },
    });
  };

  const handleRestore = async (ws: WorkspaceAssignmentDto) => {
    try {
      await restoreWorkspace(ws.assignmentId);
      showToast('success', 'Workspace restored');
    } catch {
      showToast('error', 'Could not restore the workspace. Try again.');
    }
  };

  const handleResetRequest = () => {
    setConfirmState({
      title: 'Reset all workspaces?',
      body: 'This will remove all locally configured workspaces and return KST to the empty startup screen.',
      confirmLabel: 'Reset Workspaces',
      destructive: true,
      onConfirm: async () => {
        try {
          await resetWorkspaces();
          showToast('success', 'Workspace configuration reset');
          setShowManageDialog(false);
        } catch {
          showToast('error', 'Could not reset workspace configuration. Try again.');
        } finally {
          setConfirmState(null);
        }
      },
    });
  };

  const handleReorder = async (orderedIds: string[]) => {
    try {
      await reorderWorkspaces(orderedIds);
    } catch {
      showToast('error', 'Could not reorder workspaces. Try again.');
    }
  };

  const handleRefresh = async () => {
    setIsRefreshing(true);
    try {
      await triggerRefresh();
    } finally {
      setIsRefreshing(false);
    }
  };

  const handleUpdatePreference = async (next: Partial<UserPreferencesDto>) => {
    try {
      await updatePreferences(next);
    } catch {
      showToast('error', 'Could not save preferences. Try again.');
    }
  };

  const activeWorkspace =
    !isGeneralActive && workspacesState.activeId != null
      ? activeWorkspaces.find((w) => w.assignmentId === workspacesState.activeId)
      : null;

  const version = appVersion ?? status?.applicationVersion ?? '—';

  return (
    <div
      className="shell"
      data-theme={resolvedTheme}
      data-accent={preferences.accentColor}
      data-density={preferences.rowDensity}
    >
      <TopApplicationBar
        version={version}
        connectionState={connectionState}
        configurationWarning={workspacesState.configurationWarning}
      />

      <WorkspaceTabBar
        workspaces={activeWorkspaces}
        activeId={isGeneralActive ? null : workspacesState.activeId}
        onSelect={handleSelectWorkspace}
        onAdd={() => setShowAddDialog(true)}
        onManage={() => setShowManageDialog(true)}
        onEdit={(ws) => setEditingWorkspace(ws)}
        onArchive={handleArchiveRequest}
        onDelete={handleDeleteRequest}
        onReorder={handleReorder}
        isGeneralActive={isGeneralActive}
        onSelectGeneral={handleSelectGeneral}
      />

      <main className="shell__content">
        {isGeneralActive ? (
          <GeneralWorkspace
            preferences={preferences}
            onSetTheme={(theme) => handleUpdatePreference({ theme })}
            onSetAccentColor={(accentColor) => handleUpdatePreference({ accentColor })}
            onSetRowDensity={(rowDensity) => handleUpdatePreference({ rowDensity })}
            onOpenManageWorkspaces={() => setShowManageDialog(true)}
            connectionState={connectionState}
            status={status}
            onRefresh={handleRefresh}
            isRefreshing={isRefreshing}
          />
        ) : activeWorkspaces.length === 0 ? (
          <EmptyWorkspace />
        ) : activeWorkspace ? (
          <MpsWorkspace workspace={activeWorkspace} />
        ) : null}
      </main>

      <BottomStatusBar
        connectionState={connectionState}
        status={status}
        onRefresh={handleRefresh}
        isRefreshing={isRefreshing}
      />

      {(showAddDialog || editingWorkspace) && (
        <AddWorkspaceDialog
          workspace={editingWorkspace ?? undefined}
          onSave={editingWorkspace ? handleEditSave : handleAddWorkspace}
          onClose={() => {
            setShowAddDialog(false);
            setEditingWorkspace(null);
          }}
        />
      )}

      {showManageDialog && (
        <ManageWorkspacesDialog
          activeWorkspaces={activeWorkspaces}
          archivedWorkspaces={archivedWorkspaces}
          onRestore={handleRestore}
          onDelete={handleDeleteRequest}
          onResetRequest={handleResetRequest}
          onClose={() => setShowManageDialog(false)}
        />
      )}

      {confirmState && (
        <ConfirmDialog
          title={confirmState.title}
          body={confirmState.body}
          confirmLabel={confirmState.confirmLabel}
          destructive={confirmState.destructive}
          onConfirm={confirmState.onConfirm}
          onCancel={() => setConfirmState(null)}
        />
      )}

      <ToastStack toasts={toasts} onDismiss={dismissToast} />
    </div>
  );
}

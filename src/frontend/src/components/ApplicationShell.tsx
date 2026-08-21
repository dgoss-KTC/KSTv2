import { useState, useEffect, useRef } from 'react';
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

// Focuses `el` if it still exists in the document, returning whether it did. Used for dialog
// focus restoration so a disconnected (e.g. deleted/unmounted) trigger is never focused.
function focusIfConnected(el: HTMLElement | null): boolean {
  if (el && el.isConnected) {
    el.focus();
    return true;
  }
  return false;
}

// Identifies which of the three stacked-capable workspace dialogs is currently topmost, so each
// dialog's Escape handler can be gated to act only when it owns the keyboard — ConfirmDialog can
// be opened on top of ManageWorkspacesDialog (Delete/Reset), and only one Escape press should ever
// close one dialog.
type TopmostDialog = 'add' | 'manage' | 'confirm' | null;

export function ApplicationShell({ appVersion }: { appVersion?: string }) {
  const [isGeneralActive, setIsGeneralActive] = useState(false);
  const [showAddDialog, setShowAddDialog] = useState(false);
  const [editingWorkspace, setEditingWorkspace] = useState<WorkspaceAssignmentDto | null>(null);
  const [showManageDialog, setShowManageDialog] = useState(false);
  const [confirmState, setConfirmState] = useState<ConfirmState | null>(null);
  const [isRefreshing, setIsRefreshing] = useState(false);

  // Focus-restoration targets are DOM elements, not application state — stored in refs per the
  // repository focus-management convention (see Stage 8D.6 Component Information).
  const addDialogReturnFocusRef = useRef<HTMLElement | null>(null);
  const manageDialogReturnFocusRef = useRef<HTMLElement | null>(null);
  const confirmReturnFocusRef = useRef<HTMLElement | null>(null);
  const manageDialogContainerRef = useRef<HTMLDivElement>(null);

  // Mirrors of each dialog's own local busy/saving state, updated synchronously via callback props
  // (not React state) so the single central Escape listener below can read the current value
  // without waiting for a render. See the workspace-dialog Escape correction note in section 2.
  const confirmBusyRef = useRef(false);
  const addDialogSavingRef = useRef(false);

  const topmostDialog: TopmostDialog = confirmState
    ? 'confirm'
    : showManageDialog
      ? 'manage'
      : showAddDialog || editingWorkspace
        ? 'add'
        : null;

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

  const handleOpenAddDialog = (triggerEl: HTMLElement) => {
    addDialogSavingRef.current = false;
    addDialogReturnFocusRef.current = triggerEl;
    setShowAddDialog(true);
  };

  const handleOpenEditDialog = (ws: WorkspaceAssignmentDto, triggerEl: HTMLElement | null) => {
    addDialogSavingRef.current = false;
    addDialogReturnFocusRef.current = triggerEl;
    setEditingWorkspace(ws);
  };

  // Shared by Escape, backdrop-cancel, and successful save — every path that closes the Add/Edit
  // Workspace dialog restores focus to whatever opened it (the "+" button, or the stable workspace
  // kebab trigger for edit mode).
  const closeAddDialog = () => {
    setShowAddDialog(false);
    setEditingWorkspace(null);
    const el = addDialogReturnFocusRef.current;
    addDialogReturnFocusRef.current = null;
    focusIfConnected(el);
  };

  const handleAddWorkspace = async (fields: Parameters<typeof addWorkspace>[0]) => {
    await addWorkspace(fields);
    closeAddDialog();
  };

  const handleEditSave = async (fields: Parameters<typeof addWorkspace>[0]) => {
    if (!editingWorkspace) return;
    await editWorkspace(editingWorkspace.assignmentId, fields);
    closeAddDialog();
    showToast('success', 'Workspace updated');
  };

  const handleOpenManageDialog = (triggerEl: HTMLElement) => {
    manageDialogReturnFocusRef.current = triggerEl;
    setShowManageDialog(true);
  };

  const handleCloseManageDialog = () => {
    setShowManageDialog(false);
    const el = manageDialogReturnFocusRef.current;
    manageDialogReturnFocusRef.current = null;
    focusIfConnected(el);
  };

  // Shared by ConfirmDialog Escape/Cancel/backdrop and by every destructive action's completion.
  // Prefers the exact triggering control; if it no longer exists (e.g. the row it belonged to was
  // just deleted), falls back to focus remaining inside Manage Workspaces when that dialog is still
  // open beneath it, rather than focusing a disconnected element or the application shell.
  //
  // Deferred one macrotask: a successful destructive action (delete/reset) triggers a workspace-
  // list state update in the same tick, which can remove the triggering element from the DOM only
  // after this function starts running. Checking connectivity synchronously would then focus an
  // element that is about to be unmounted, and its removal drops focus back to <body>. Waiting for
  // that pending re-render to commit first makes the connectivity check reflect the final DOM.
  const restoreFocusAfterConfirmClose = () => {
    const el = confirmReturnFocusRef.current;
    confirmReturnFocusRef.current = null;
    const wasManageDialogOpen = showManageDialog;
    setTimeout(() => {
      if (focusIfConnected(el)) return;
      if (wasManageDialogOpen) {
        manageDialogContainerRef.current?.focus();
      }
    }, 0);
  };

  const handleCancelConfirm = () => {
    setConfirmState(null);
    restoreFocusAfterConfirmClose();
  };

  // Single document-level, capture-phase Escape listener that arbitrates across every stacked
  // workspace dialog (Add/Edit, Manage, Confirm). Centralized here rather than as independent
  // per-dialog listeners so a single Escape press can never be ambiguous about which dialog it
  // closes and does not depend on listener registration order, DOM bubbling, or which control
  // currently has focus. ComponentInfoModal is unrelated (BOM-only, not a workspace dialog) and
  // keeps its own independent Escape handling.
  useEffect(() => {
    function handleDocumentEscape(e: KeyboardEvent) {
      if (e.key !== 'Escape') return;
      if (topmostDialog === 'confirm') {
        if (confirmBusyRef.current) return;
        e.preventDefault();
        e.stopPropagation();
        handleCancelConfirm();
      } else if (topmostDialog === 'add') {
        if (addDialogSavingRef.current) return;
        e.preventDefault();
        e.stopPropagation();
        closeAddDialog();
      } else if (topmostDialog === 'manage') {
        e.preventDefault();
        e.stopPropagation();
        handleCloseManageDialog();
      }
    }
    document.addEventListener('keydown', handleDocumentEscape, true);
    return () => document.removeEventListener('keydown', handleDocumentEscape, true);
  });

  const handleArchiveRequest = (ws: WorkspaceAssignmentDto, triggerEl: HTMLElement | null) => {
    confirmBusyRef.current = false;
    confirmReturnFocusRef.current = triggerEl;
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
          restoreFocusAfterConfirmClose();
        }
      },
    });
  };

  const handleDeleteRequest = (ws: WorkspaceAssignmentDto, triggerEl: HTMLElement | null) => {
    confirmBusyRef.current = false;
    confirmReturnFocusRef.current = triggerEl;
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
          restoreFocusAfterConfirmClose();
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

  const handleResetRequest = (triggerEl: HTMLElement) => {
    confirmBusyRef.current = false;
    confirmReturnFocusRef.current = triggerEl;
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
          restoreFocusAfterConfirmClose();
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
        onAdd={handleOpenAddDialog}
        onManage={handleOpenManageDialog}
        onEdit={handleOpenEditDialog}
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
            onOpenManageWorkspaces={handleOpenManageDialog}
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
          onClose={closeAddDialog}
          onSavingChange={(saving) => {
            addDialogSavingRef.current = saving;
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
          onClose={handleCloseManageDialog}
          containerRef={manageDialogContainerRef}
        />
      )}

      {confirmState && (
        <ConfirmDialog
          title={confirmState.title}
          body={confirmState.body}
          confirmLabel={confirmState.confirmLabel}
          destructive={confirmState.destructive}
          onConfirm={confirmState.onConfirm}
          onCancel={handleCancelConfirm}
          onBusyChange={(busy) => {
            confirmBusyRef.current = busy;
          }}
        />
      )}

      <ToastStack toasts={toasts} onDismiss={dismissToast} />
    </div>
  );
}

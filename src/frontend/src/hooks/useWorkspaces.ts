import { useState, useCallback, useMemo } from 'react';
import type { WorkspaceAssignmentDto } from '../api/client';
import {
  fetchWorkspaces,
  createWorkspace,
  updateWorkspace as updateWorkspaceApi,
  archiveWorkspace as archiveWorkspaceApi,
  restoreWorkspace as restoreWorkspaceApi,
  deleteWorkspace as deleteWorkspaceApi,
  resetWorkspaces as resetWorkspacesApi,
  reorderWorkspaces as reorderWorkspacesApi,
  type CreateWorkspaceFields,
  type WorkspaceApiError,
} from '../api/workspaceApi';

export interface WorkspacesState {
  workspaces: WorkspaceAssignmentDto[];
  activeId: string | null;
  configurationWarning: string | null;
  isLoading: boolean;
}

function sortBySortOrder(list: WorkspaceAssignmentDto[]): WorkspaceAssignmentDto[] {
  return [...list].sort((a, b) => Number(a.sortOrder) - Number(b.sortOrder));
}

/**
 * Determines the fallback active tab after a workspace is archived or deleted:
 * the next enabled workspace, else the previous enabled one, else none.
 */
function computeFallbackActiveId(
  workspacesBeforeRemoval: WorkspaceAssignmentDto[],
  removedId: string,
): string | null {
  const enabledBefore = sortBySortOrder(
    workspacesBeforeRemoval.filter((w) => w.isEnabled || w.assignmentId === removedId),
  );
  const index = enabledBefore.findIndex((w) => w.assignmentId === removedId);
  if (index === -1) return null;
  if (index < enabledBefore.length - 1) return enabledBefore[index + 1].assignmentId;
  if (index > 0) return enabledBefore[index - 1].assignmentId;
  return null;
}

export function useWorkspaces() {
  const [state, setState] = useState<WorkspacesState>({
    workspaces: [],
    activeId: null,
    configurationWarning: null,
    isLoading: true,
  });

  const load = useCallback(async () => {
    try {
      const result = await fetchWorkspaces();
      const ordered = sortBySortOrder(result.workspaces);
      const firstEnabled = ordered.find((w) => w.isEnabled) ?? null;
      setState({
        workspaces: ordered,
        activeId: firstEnabled?.assignmentId ?? null,
        configurationWarning: result.configurationWarning ?? null,
        isLoading: false,
      });
    } catch {
      setState((prev) => ({ ...prev, isLoading: false }));
    }
  }, []);

  const addWorkspace = useCallback(
    async (fields: CreateWorkspaceFields): Promise<void> => {
      // May throw WorkspaceApiError for validation failures — caller handles it.
      const created = await createWorkspace(fields);
      setState((prev) => ({
        ...prev,
        workspaces: [...prev.workspaces, created],
        activeId: created.assignmentId,
      }));
    },
    [],
  );

  const editWorkspace = useCallback(
    async (assignmentId: string, fields: CreateWorkspaceFields): Promise<void> => {
      const updated = await updateWorkspaceApi(assignmentId, fields);
      setState((prev) => ({
        ...prev,
        workspaces: prev.workspaces.map((w) => (w.assignmentId === assignmentId ? updated : w)),
      }));
    },
    [],
  );

  const archiveWorkspace = useCallback(async (assignmentId: string): Promise<void> => {
    const updated = await archiveWorkspaceApi(assignmentId);
    setState((prev) => {
      const activeId =
        prev.activeId === assignmentId
          ? computeFallbackActiveId(prev.workspaces, assignmentId)
          : prev.activeId;
      return {
        ...prev,
        workspaces: prev.workspaces.map((w) => (w.assignmentId === assignmentId ? updated : w)),
        activeId,
      };
    });
  }, []);

  const restoreWorkspace = useCallback(async (assignmentId: string): Promise<void> => {
    const updated = await restoreWorkspaceApi(assignmentId);
    setState((prev) => ({
      ...prev,
      workspaces: prev.workspaces.map((w) => (w.assignmentId === assignmentId ? updated : w)),
    }));
  }, []);

  const deleteWorkspace = useCallback(async (assignmentId: string): Promise<void> => {
    await deleteWorkspaceApi(assignmentId);
    setState((prev) => {
      const activeId =
        prev.activeId === assignmentId
          ? computeFallbackActiveId(prev.workspaces, assignmentId)
          : prev.activeId;
      return {
        ...prev,
        workspaces: prev.workspaces.filter((w) => w.assignmentId !== assignmentId),
        activeId,
      };
    });
  }, []);

  const resetWorkspaces = useCallback(async (): Promise<void> => {
    await resetWorkspacesApi();
    setState((prev) => ({ ...prev, workspaces: [], activeId: null }));
  }, []);

  const reorderWorkspaces = useCallback(
    async (orderedIds: string[]): Promise<void> => {
      const previousWorkspaces = state.workspaces;
      const byId = new Map(previousWorkspaces.map((w) => [w.assignmentId, w]));
      const reorderedEnabled = orderedIds
        .map((id, index): WorkspaceAssignmentDto | null => {
          const w = byId.get(id);
          return w ? { ...w, sortOrder: index } : null;
        })
        .filter((w): w is WorkspaceAssignmentDto => w !== null);
      const archived = sortBySortOrder(previousWorkspaces.filter((w) => !w.isEnabled));

      // Optimistic update so drag/menu reordering feels instant.
      setState((prev) => ({ ...prev, workspaces: [...reorderedEnabled, ...archived] }));

      try {
        const result = await reorderWorkspacesApi(orderedIds);
        setState((prev) => ({ ...prev, workspaces: sortBySortOrder(result.workspaces) }));
      } catch (err) {
        setState((prev) => ({ ...prev, workspaces: previousWorkspaces }));
        throw err;
      }
    },
    [state.workspaces],
  );

  const selectWorkspace = useCallback((id: string) => {
    setState((prev) => ({ ...prev, activeId: id }));
  }, []);

  const activeWorkspaces = useMemo(
    () => sortBySortOrder(state.workspaces.filter((w) => w.isEnabled)),
    [state.workspaces],
  );
  const archivedWorkspaces = useMemo(
    () => sortBySortOrder(state.workspaces.filter((w) => !w.isEnabled)),
    [state.workspaces],
  );

  return {
    state,
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
  };
}

export function isWorkspaceApiError(err: unknown): err is WorkspaceApiError & { type: 'validation' } {
  return (
    typeof err === 'object' &&
    err !== null &&
    'type' in err &&
    (err as WorkspaceApiError).type === 'validation'
  );
}

export function isWorkspaceNotFoundError(err: unknown): err is WorkspaceApiError & { type: 'not-found' } {
  return (
    typeof err === 'object' &&
    err !== null &&
    'type' in err &&
    (err as WorkspaceApiError).type === 'not-found'
  );
}

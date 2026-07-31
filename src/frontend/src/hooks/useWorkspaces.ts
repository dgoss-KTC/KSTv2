import { useState, useCallback } from 'react';
import type { WorkspaceAssignmentDto } from '../api/client';
import {
  fetchWorkspaces,
  createWorkspace,
  type CreateWorkspaceFields,
  type WorkspaceApiError,
} from '../api/workspaceApi';

export interface WorkspacesState {
  workspaces: WorkspaceAssignmentDto[];
  activeId: string | null;
  configurationWarning: string | null;
  isLoading: boolean;
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
      const ordered = [...result.workspaces].sort(
        (a, b) => Number(a.sortOrder) - Number(b.sortOrder),
      );
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

  const selectWorkspace = useCallback((id: string) => {
    setState((prev) => ({ ...prev, activeId: id }));
  }, []);

  return { state, load, addWorkspace, selectWorkspace };
}

export function isWorkspaceApiError(err: unknown): err is WorkspaceApiError {
  return (
    typeof err === 'object' &&
    err !== null &&
    'type' in err &&
    (err as WorkspaceApiError).type === 'validation'
  );
}

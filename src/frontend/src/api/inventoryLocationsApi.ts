import { ApiClient, type InventoryLocationsResponseDto } from './client';
import { resolveBackendBaseUrl } from './tauri-bridge';

export async function fetchInventoryLocations(assignmentId: string, part: string, snapshotId: string, signal: AbortSignal): Promise<InventoryLocationsResponseDto> {
  const client = new ApiClient(await resolveBackendBaseUrl());
  signal.throwIfAborted();
  const result = await client.getInventoryLocations(assignmentId, part, snapshotId, signal);
  if (!result || result.workspaceId !== assignmentId || result.mpsSnapshotId !== snapshotId ||
      result.componentPart.toUpperCase() !== part.trim().toUpperCase() || !Array.isArray(result.rows) ||
      typeof result.site !== 'string' || typeof result.acquiredAtUtc !== 'string' || typeof result.isStale !== 'boolean' ||
      result.rows.some(row => typeof row.rowId !== 'string' ||
        ![row.location, row.lotSerial, row.status].every(value => value === null || typeof value === 'string') ||
        !(row.quantityOnHand === null || (typeof row.quantityOnHand === 'string' && /^[+-]?\d+(?:\.\d+)?$/.test(row.quantityOnHand)))))
    throw new Error('Unexpected inventory locations response.');
  return result;
}

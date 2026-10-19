import type { components } from '../generated/api';
import { resolveBackendBaseUrl } from './tauri-bridge';
import { isRunningInTauri } from './tauri-bridge';

export type ComponentMrpRequest = components['schemas']['ComponentMrpRequestDto'];
export type ComponentMrpPrepared = components['schemas']['ComponentMrpPreparedDto'];

export async function generateComponentMrp(request: ComponentMrpRequest, signal?: AbortSignal, ownedEpoch?: number): Promise<ComponentMrpPrepared> {
  if (isRunningInTauri()) {
    const { invoke } = await import('@tauri-apps/api/core');
    const epoch = ownedEpoch ?? await invoke<number>('component_mrp_begin');
    signal?.throwIfAborted();
    const invalidate = () => { void invoke('sop_discard_generation', { epoch }).catch(() => {}); };
    signal?.addEventListener('abort', invalidate, { once: true });
    try { return await invoke<ComponentMrpPrepared>('component_mrp_generate', { epoch, ...request }); }
    finally { signal?.removeEventListener('abort', invalidate); }
  }
  const response = await fetch(`${await resolveBackendBaseUrl()}/api/v1/reports/component-mrp`, {
    method: 'POST', headers: { 'Content-Type': 'application/json', 'X-KST-Report-Session': crypto.randomUUID() }, body: JSON.stringify(request), signal,
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string } | null;
    throw new Error(problem?.detail ?? 'Component MRP preparation failed.');
  }
  return response.json() as Promise<ComponentMrpPrepared>;
}

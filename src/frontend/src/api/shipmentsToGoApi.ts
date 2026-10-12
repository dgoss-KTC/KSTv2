import type { components } from '../generated/api';
import { isRunningInTauri, resolveBackendBaseUrl } from './tauri-bridge';

export type ShipmentsToGoRequest = components['schemas']['ShipmentsToGoRequestDto'];
export type ShipmentsToGoPrepared = components['schemas']['ShipmentsToGoPreparedDto'];

export async function generateShipmentsToGo(request: ShipmentsToGoRequest, signal?: AbortSignal): Promise<ShipmentsToGoPrepared> {
  if (isRunningInTauri()) {
    const { invoke } = await import('@tauri-apps/api/core');
    const epoch = await invoke<number>('component_mrp_begin');
    const invalidate = () => { void invoke('report_discard').catch(() => {}); };
    signal?.addEventListener('abort', invalidate, { once: true });
    try {
      return await invoke('component_mrp_generate', { epoch, reportKind: 'ShipmentsToGo', ...request,
        site: '', part: '', horizonWeeks: 26, includeUnconfirmed: true, period: 'Week' });
    } finally { signal?.removeEventListener('abort', invalidate); }
  }
  const response = await fetch(`${await resolveBackendBaseUrl()}/api/v1/reports/shipments-to-go`, {
    method: 'POST', headers: { 'Content-Type': 'application/json', 'X-KST-Report-Session': crypto.randomUUID() }, body: JSON.stringify(request), signal,
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string } | null;
    throw new Error(problem?.detail ?? 'Shipments-To-Go preparation failed.');
  }
  return response.json() as Promise<ShipmentsToGoPrepared>;
}

import type { components } from '../generated/api';
import { isRunningInTauri, resolveBackendBaseUrl } from './tauri-bridge';

export type OpenOrderReportRequest = components['schemas']['OpenOrderReportRequestDto'];
export type OpenOrderReportPrepared = components['schemas']['OpenOrderReportPreparedDto'];

export async function generateOpenOrderReport(request: OpenOrderReportRequest, signal?: AbortSignal, ownedEpoch?: number): Promise<OpenOrderReportPrepared> {
  signal?.throwIfAborted();
  if (isRunningInTauri()) {
    const { invoke } = await import('@tauri-apps/api/core');
    const epoch = ownedEpoch ?? await invoke<number>('component_mrp_begin');
    signal?.throwIfAborted();
    const invalidate = () => { void invoke('sop_discard_generation', { epoch }).catch(() => {}); };
    signal?.addEventListener('abort', invalidate, { once: true });
    try {
      const result = await invoke<OpenOrderReportPrepared>('component_mrp_generate', { epoch, reportKind: 'OpenOrderReport', oorRequest: request,
        site: '', part: '', horizonWeeks: 26, includeUnconfirmed: true, period: 'Week' });
      signal?.throwIfAborted(); return result;
    } finally { signal?.removeEventListener('abort', invalidate); }
  }
  const response = await fetch(`${await resolveBackendBaseUrl()}/api/v1/reports/open-orders`, {
    method: 'POST', headers: { 'Content-Type': 'application/json', 'X-KST-Report-Session': crypto.randomUUID() }, body: JSON.stringify(request), signal,
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; errors?: Record<string, string[]> } | null;
    throw new Error(problem?.detail ?? (Object.values(problem?.errors ?? {}).flat().join(' ') || 'Open Order Report preparation failed.'));
  }
  return response.json() as Promise<OpenOrderReportPrepared>;
}

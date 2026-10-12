import type { components } from '../generated/api';
import { isRunningInTauri, resolveBackendBaseUrl } from './tauri-bridge';
import { sopCalendarFingerprint, sopPresentation } from '../reports/sopPresentation';

export type SopRequest = components['schemas']['SopRequestDto'];
export type SopPrepared = components['schemas']['SopPreparedDto'];
type Acquired = components['schemas']['SopAcquiredDto'];

export async function generateSop(request: SopRequest, signal?: AbortSignal): Promise<SopPrepared> {
  const fingerprint = sopCalendarFingerprint();
  let acquired: Acquired | undefined;
  const owner = crypto.randomUUID();
  const desktop = isRunningInTauri();
  const { invoke } = await import('@tauri-apps/api/core');
  const epoch = desktop ? await invoke<number>('component_mrp_begin') : 0;
  const base = desktop ? '' : await resolveBackendBaseUrl();
  const check = () => { signal?.throwIfAborted(); if (fingerprint !== sopCalendarFingerprint()) throw new Error('Fiscal calendar changed. Generate again.'); };
  const discard = () => { if (desktop) void invoke('sop_discard_generation', { epoch }).catch(() => {}); };
  signal?.addEventListener('abort', discard, { once: true });
  async function post<T>(path: string, value: unknown): Promise<T> {
    const response = await fetch(`${base}/api/v1/reports/sop/${path}`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-KST-Report-Session': owner }, body: JSON.stringify(value), signal });
    if (!response.ok) { const p = await response.json().catch(() => null) as { detail?: string } | null; throw new Error(p?.detail ?? 'S&OP generation failed.'); }
    return response.json() as Promise<T>;
  }
  try {
    check();
    const hostArgs = { epoch, reportKind: 'Sop', site: request.site, part: '', horizonWeeks: 26, includeUnconfirmed: true, period: 'Week' };
    acquired = desktop ? await invoke<Acquired>('component_mrp_generate', { ...hostArgs, sopRequest: request }) : await post<Acquired>('acquisitions', request);
    check();
    if (acquired.outcome === 'noData') return { ...acquired, preparationId: null, fileName: null };
    if (!acquired.acquisitionId) throw new Error('Invalid S&OP acquisition.');
    const prepare: components['schemas']['SopPrepareDto'] = { acquisitionId: acquired.acquisitionId, presentation: sopPresentation(acquired.weekLabels) };
    const result = desktop ? await invoke<SopPrepared>('component_mrp_generate', { ...hostArgs, sopPrepare: prepare }) : await post<SopPrepared>('preparations', prepare);
    check(); return result;
  } catch (error) {
    if (desktop) await invoke('sop_discard_generation', { epoch }).catch(() => {});
    throw error;
  } finally {
    signal?.removeEventListener('abort', discard);
    if (acquired?.acquisitionId) {
      if (desktop) await invoke('sop_discard_acquisition', { acquisitionId: acquired.acquisitionId }).catch(() => {});
      else await fetch(`${base}/api/v1/reports/sop/acquisitions/${acquired.acquisitionId}`, { method: 'DELETE', headers: { 'X-KST-Report-Session': owner } }).catch(() => {});
    }
  }
}

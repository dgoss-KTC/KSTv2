import type { components } from '../generated/api';
import { resolveBackendBaseUrl } from './tauri-bridge';
import { isRunningInTauri } from './tauri-bridge';
import { invoke } from '@tauri-apps/api/core';

export type ComponentMrpRequest = components['schemas']['ComponentMrpRequestDto'];
export type ComponentMrpPrepared = components['schemas']['ComponentMrpPreparedDto'];
export type ComponentMrpScreen = components['schemas']['ComponentMrpScreenDto'];
// Browser development only; the managed host's stable session never enters the webview.
let browserSession: string | undefined;
const browserOwner = () => browserSession ??= crypto.randomUUID();
// Abort invalidates the native epoch. Wait for the old close lease to finish before
// dispatching the newest state; aborted intermediate queue entries never reach the host.
let screenQueue: Promise<unknown> = Promise.resolve();
function serialize<T>(run: () => Promise<T>): Promise<T> {
  const pending = screenQueue.then(run, run); screenQueue = pending.catch(() => {}); return pending;
}
export async function readComponentMrp(request: ComponentMrpRequest, signal?: AbortSignal): Promise<ComponentMrpScreen> {
  const run = async () => {
    signal?.throwIfAborted();
    if (!isRunningInTauri()) return post<ComponentMrpScreen>('component-mrp/projection', request, signal);
    const epoch = await invoke<number>('component_mrp_begin');
    const invalidate = () => { void invoke('sop_discard_generation', { epoch }).catch(() => {}); };
    if (signal?.aborted) { invalidate(); signal.throwIfAborted(); }
    signal?.addEventListener('abort', invalidate, { once: true });
    try {
      const result = await invoke<ComponentMrpScreen>('component_mrp_generate', { epoch, ...request, screenRead: true });
      signal?.throwIfAborted(); return result;
    } finally { signal?.removeEventListener('abort', invalidate); }
  };
  return serialize(run);
}
async function post<T>(route: string, request: ComponentMrpRequest, signal?: AbortSignal): Promise<T> {
  const response = await fetch(`${await resolveBackendBaseUrl()}/api/v1/reports/${route}`, {
    method: 'POST', headers: { 'Content-Type': 'application/json', 'X-KST-Report-Session': browserOwner() }, body: JSON.stringify(request), signal,
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string } | null;
    throw new Error(problem?.detail ?? 'Component MRP acquisition/preparation failed.');
  }
  return response.json() as Promise<T>;
}

export async function generateComponentMrp(request: ComponentMrpRequest, signal?: AbortSignal, ownedEpoch?: number): Promise<ComponentMrpPrepared> {
  return serialize(async () => {
    signal?.throwIfAborted();
    if (isRunningInTauri()) {
      const epoch = ownedEpoch ?? await invoke<number>('component_mrp_begin');
      const invalidate = () => { void invoke('sop_discard_generation', { epoch }).catch(() => {}); };
      if (signal?.aborted) { invalidate(); signal.throwIfAborted(); }
      signal?.addEventListener('abort', invalidate, { once: true });
      try {
        const result = await invoke<ComponentMrpPrepared>('component_mrp_generate', { epoch, ...request });
        signal?.throwIfAborted(); return result;
      } finally { signal?.removeEventListener('abort', invalidate); }
    }
    return post<ComponentMrpPrepared>(request.acquisitionId ? 'component-mrp/screen-preparations' : 'component-mrp', request, signal);
  });
}

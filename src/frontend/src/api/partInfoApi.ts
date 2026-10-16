import type { components } from '../generated/api';
import { resolveBackendBaseUrl } from './tauri-bridge';

export type PartInfo = components['schemas']['PartInfoResponseDto'];
export type PartInfoSpec = components['schemas']['PartInfoSpecDto'];
export type PartInfoLocations = components['schemas']['PartInfoLocationsDto'];
export async function refreshPartInfoLocations(site: string, part: string, signal?: AbortSignal): Promise<PartInfoLocations> {
  signal?.throwIfAborted();
  const response = await fetch(`${await resolveBackendBaseUrl()}/api/v1/reports/part-info/inventory-locations?${new URLSearchParams({ site, part })}`, { signal });
  if (!response.ok) throw new Error('Inventory locations could not be acquired. Retry inventory.');
  const result = await response.json() as PartInfoLocations;
  signal?.throwIfAborted();
  if (result.site !== site || result.part !== part) throw new Error('Inventory lookup identity mismatch.');
  return result;
}
export class PartInfoError extends Error {
  constructor(public readonly status: number, message: string) { super(message); }
}
export async function getPartInfoSpec(signal?: AbortSignal): Promise<PartInfoSpec> {
  const response = await fetch(`${await resolveBackendBaseUrl()}/api/v1/reports/part-info/spec`, { signal });
  if (!response.ok) throw new Error('Lookup settings unavailable.');
  return response.json() as Promise<PartInfoSpec>;
}
export async function lookupPartInfo(site: string, part: string, signal?: AbortSignal): Promise<PartInfo> {
  signal?.throwIfAborted();
  const response = await fetch(`${await resolveBackendBaseUrl()}/api/v1/reports/part-info?${new URLSearchParams({ site, part })}`, { signal });
  if (!response.ok) throw new PartInfoError(response.status, response.status === 404
    ? 'No part master was found in the selected site’s domain.'
    : response.status === 400 ? 'Enter one exact part number and a supported site.' : 'Part information could not be acquired. Please retry.');
  const result = await response.json() as PartInfo;
  signal?.throwIfAborted();
  if (result.site !== site || result.part !== part) throw new PartInfoError(503, 'Lookup identity did not match the request. Please retry.');
  return result;
}

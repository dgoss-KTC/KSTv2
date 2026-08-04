import { ApiClient, type PreferencesResponseDto, type UpdatePreferencesRequestDto } from './client';
import { resolveBackendBaseUrl } from './tauri-bridge';

export async function fetchPreferences(): Promise<PreferencesResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  return client.getPreferences();
}

export async function updatePreferences(
  request: UpdatePreferencesRequestDto,
): Promise<PreferencesResponseDto> {
  const baseUrl = await resolveBackendBaseUrl();
  const client = new ApiClient(baseUrl);
  return client.updatePreferences(request);
}

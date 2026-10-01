/**
 * Typed API client wrapping generated OpenAPI types.
 * The base URL is provided at runtime by Tauri after the backend starts.
 */
import type { components } from '../generated/api';

export type SystemStatusResponse = components['schemas']['SystemStatusResponse'];
export type HealthResponse = components['schemas']['HealthResponse'];
export type ReadyResponse = components['schemas']['ReadyResponse'];
export type WorkspaceAssignmentDto = components['schemas']['WorkspaceAssignmentDto'];
export type WorkspaceListResponseDto = components['schemas']['WorkspaceListResponseDto'];
export type CreateWorkspaceRequestDto = components['schemas']['CreateWorkspaceRequestDto'];
export type ReorderWorkspacesRequestDto = components['schemas']['ReorderWorkspacesRequestDto'];
export type UserPreferencesDto = components['schemas']['UserPreferencesDto'];
export type PreferencesResponseDto = components['schemas']['PreferencesResponseDto'];
export type UpdatePreferencesRequestDto = components['schemas']['UpdatePreferencesRequestDto'];
export type MpsDashboardResponseDto = components['schemas']['MpsDashboardResponseDto'];
export type MpsSnapshotMetadataDto = components['schemas']['MpsSnapshotMetadataDto'];
export type MpsPartScheduleDto = components['schemas']['MpsPartScheduleDto'];
export type MpsBucketDto = components['schemas']['MpsBucketDto'];
export type PartDetailResponseDto = components['schemas']['PartDetailResponseDto'];
export type PartPriceBreakDto = components['schemas']['PartPriceBreakDto'];
export type WorkOrderPlanningWindowResponseDto = components['schemas']['WorkOrderPlanningWindowResponseDto'];
export type WorkOrderSummaryDto = components['schemas']['WorkOrderSummaryDto'];
export type KittingSummaryDto = components['schemas']['KittingSummaryDto'];
export type WorkOrderMaterialResponseDto = components['schemas']['WorkOrderMaterialResponseDto'];
export type WorkOrderMaterialLineDto = components['schemas']['WorkOrderMaterialLineDto'];
export type WorkOrderCandidateResponseDto = components['schemas']['WorkOrderCandidateResponseDto'];
export type WorkOrderImmediateMaterialAnalysisResponseDto = components['schemas']['WorkOrderImmediateMaterialAnalysisResponseDto'];
export type WorkOrderImmediateMaterialComponentDto = components['schemas']['WorkOrderImmediateMaterialComponentDto'];
export type WorkOrderImmediateMaterialSummaryResponseDto = components['schemas']['WorkOrderImmediateMaterialSummaryResponseDto'];
export type BomLineDto = components['schemas']['BomLineDto'];
export type BomResponseDto = components['schemas']['BomResponseDto'];
export type ComponentDetailResponseDto = components['schemas']['ComponentDetailResponseDto'];
export type ApprovedVendorDto = components['schemas']['ApprovedVendorDto'];
export type ComponentOrdersResponseDto = components['schemas']['ComponentOrdersResponseDto'];
export type LongTermShortagesResponseDto = components['schemas']['LongTermShortagesResponseDto'];
export type LongTermShortagePurchasingDto = components['schemas']['LongTermShortagePurchasingDto'];
export type ExportLongTermShortagesRequestDto = components['schemas']['ExportLongTermShortagesRequestDto'];
export type OpenOrdersResponseDto = components['schemas']['OpenOrdersResponseDto'];
export type ExportOpenOrdersReportRequestDto = components['schemas']['ExportOpenOrdersReportRequestDto'];
export type OpenOrdersDraftResponseDto = components['schemas']['OpenOrdersDraftResponseDto'];
export type OpenOrdersDraftPresenceDto = components['schemas']['OpenOrdersDraftPresenceDto'];
export type SaveOpenOrdersDraftRequestDto = components['schemas']['SaveOpenOrdersDraftRequestDto'];

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly url: string,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export class ApiClient {
  private readonly baseUrl: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl.replace(/\/$/, '');
  }

  async getHealth(): Promise<HealthResponse> {
    return this.get<HealthResponse>('/health');
  }

  async getReady(): Promise<ReadyResponse> {
    return this.get<ReadyResponse>('/ready');
  }

  async getSystemStatus(): Promise<SystemStatusResponse> {
    return this.get<SystemStatusResponse>('/api/v1/system/status');
  }

  async listWorkspaces(): Promise<WorkspaceListResponseDto> {
    return this.get<WorkspaceListResponseDto>('/api/v1/workspaces');
  }

  async createWorkspace(request: CreateWorkspaceRequestDto): Promise<WorkspaceAssignmentDto> {
    return this.post<WorkspaceAssignmentDto>('/api/v1/workspaces', request);
  }

  async updateWorkspace(
    assignmentId: string,
    request: CreateWorkspaceRequestDto,
  ): Promise<WorkspaceAssignmentDto> {
    return this.put<WorkspaceAssignmentDto>(`/api/v1/workspaces/${assignmentId}`, request);
  }

  async archiveWorkspace(assignmentId: string): Promise<WorkspaceAssignmentDto> {
    return this.postEmpty<WorkspaceAssignmentDto>(`/api/v1/workspaces/${assignmentId}/archive`);
  }

  async restoreWorkspace(assignmentId: string): Promise<WorkspaceAssignmentDto> {
    return this.postEmpty<WorkspaceAssignmentDto>(`/api/v1/workspaces/${assignmentId}/restore`);
  }

  async deleteWorkspace(assignmentId: string): Promise<void> {
    return this.delete(`/api/v1/workspaces/${assignmentId}`);
  }

  async resetWorkspaces(): Promise<void> {
    return this.delete('/api/v1/workspaces');
  }

  async reorderWorkspaces(request: ReorderWorkspacesRequestDto): Promise<WorkspaceListResponseDto> {
    return this.put<WorkspaceListResponseDto>('/api/v1/workspaces/order', request);
  }

  async getPreferences(): Promise<PreferencesResponseDto> {
    return this.get<PreferencesResponseDto>('/api/v1/preferences');
  }

  async updatePreferences(request: UpdatePreferencesRequestDto): Promise<PreferencesResponseDto> {
    return this.put<PreferencesResponseDto>('/api/v1/preferences', request);
  }

  async refreshSystem(): Promise<SystemStatusResponse> {
    return this.postEmpty<SystemStatusResponse>('/api/v1/system/refresh');
  }

  async getMpsDashboard(
    assignmentId: string,
    dateBasis: string,
    horizonWeeks: number,
  ): Promise<MpsDashboardResponseDto> {
    const query = `?dateBasis=${encodeURIComponent(dateBasis)}&horizonWeeks=${horizonWeeks}`;
    return this.get<MpsDashboardResponseDto>(`/api/v1/workspaces/${assignmentId}/mps${query}`);
  }

  async refreshMpsDashboard(
    assignmentId: string,
    dateBasis: string,
    horizonWeeks: number,
  ): Promise<MpsDashboardResponseDto> {
    const query = `?dateBasis=${encodeURIComponent(dateBasis)}&horizonWeeks=${horizonWeeks}`;
    return this.postEmpty<MpsDashboardResponseDto>(`/api/v1/workspaces/${assignmentId}/mps/refresh${query}`);
  }

  async getPartDetail(assignmentId: string, partNumber: string): Promise<PartDetailResponseDto> {
    const query = `?partNumber=${encodeURIComponent(partNumber)}`;
    return this.get<PartDetailResponseDto>(`/api/v1/workspaces/${assignmentId}/part-detail${query}`);
  }

  async getBom(assignmentId: string, parentPart: string): Promise<BomResponseDto> {
    return this.get<BomResponseDto>(
      `/api/v1/workspaces/${assignmentId}/parts/${encodeURIComponent(parentPart)}/bom`,
    );
  }

  async getComponentDetail(assignmentId: string, componentPart: string): Promise<ComponentDetailResponseDto> {
    return this.get<ComponentDetailResponseDto>(
      `/api/v1/workspaces/${assignmentId}/components/${encodeURIComponent(componentPart)}`,
    );
  }

  async getApprovedVendors(assignmentId: string, componentPart: string): Promise<ApprovedVendorDto[]> {
    return this.get<ApprovedVendorDto[]>(
      `/api/v1/workspaces/${assignmentId}/components/${encodeURIComponent(componentPart)}/approved-vendors`,
    );
  }

  async getPlanningWindowWorkOrders(
    assignmentId: string,
    snapshotId: string,
    parentPart: string,
    dateBasis: string,
    bucketKind?: 'falldown' | 'weekly',
    weekLabel?: string,
  ): Promise<WorkOrderPlanningWindowResponseDto> {
    let query =
      `?snapshotId=${encodeURIComponent(snapshotId)}` +
      `&parentPart=${encodeURIComponent(parentPart)}` +
      `&dateBasis=${encodeURIComponent(dateBasis)}`;
    if (bucketKind) {
      query += `&bucketKind=${encodeURIComponent(bucketKind)}`;
      if (bucketKind === 'weekly' && weekLabel) {
        query += `&weekLabel=${encodeURIComponent(weekLabel)}`;
      }
    }
    return this.get<WorkOrderPlanningWindowResponseDto>(
      `/api/v1/workspaces/${assignmentId}/work-orders/planning-window${query}`,
    );
  }

  async getWorkOrderMaterialLines(
    assignmentId: string,
    snapshotId: string,
    woid: string,
  ): Promise<WorkOrderMaterialResponseDto> {
    const query = `?snapshotId=${encodeURIComponent(snapshotId)}`;
    return this.get<WorkOrderMaterialResponseDto>(
      `/api/v1/workspaces/${assignmentId}/work-orders/${encodeURIComponent(woid)}/material${query}`,
    );
  }

  async getWorkOrderImmediateMaterial(
    assignmentId: string,
    snapshotId: string,
    woid: string,
    dateBasis: string,
  ): Promise<WorkOrderImmediateMaterialAnalysisResponseDto> {
    const query = `?snapshotId=${encodeURIComponent(snapshotId)}&dateBasis=${encodeURIComponent(dateBasis)}`;
    return this.get<WorkOrderImmediateMaterialAnalysisResponseDto>(
      `/api/v1/workspaces/${assignmentId}/work-orders/${encodeURIComponent(woid)}/immediate-material${query}`,
    );
  }

  async getWorkOrderImmediateMaterialSummary(
    assignmentId: string,
    snapshotId: string,
    parentPart: string | undefined,
    dateBasis: string,
  ): Promise<WorkOrderImmediateMaterialSummaryResponseDto> {
    const parentQuery = parentPart ? `&parentPart=${encodeURIComponent(parentPart)}` : '';
    const query = `?snapshotId=${encodeURIComponent(snapshotId)}${parentQuery}&dateBasis=${encodeURIComponent(dateBasis)}`;
    return this.get<WorkOrderImmediateMaterialSummaryResponseDto>(
      `/api/v1/workspaces/${assignmentId}/work-orders/immediate-material-summary${query}`,
    );
  }

  async getWorkOrderCandidates(
    assignmentId: string,
    snapshotId: string,
    immediateParentWoid: string,
    componentPart: string,
    targetDepth: number,
    dateBasis: string,
  ): Promise<WorkOrderCandidateResponseDto> {
    const query =
      `?snapshotId=${encodeURIComponent(snapshotId)}` +
      `&immediateParentWoid=${encodeURIComponent(immediateParentWoid)}` +
      `&componentPart=${encodeURIComponent(componentPart)}` +
      `&targetDepth=${targetDepth}` +
      `&dateBasis=${encodeURIComponent(dateBasis)}`;
    return this.get<WorkOrderCandidateResponseDto>(`/api/v1/workspaces/${assignmentId}/work-orders/candidates${query}`);
  }

  async getComponentOrders(
    assignmentId: string,
    snapshotId: string,
  ): Promise<ComponentOrdersResponseDto> {
    const query = `?snapshotId=${encodeURIComponent(snapshotId)}`;
    return this.get<ComponentOrdersResponseDto>(
      `/api/v1/workspaces/${assignmentId}/component-orders${query}`,
    );
  }

  async getOpenOrders(assignmentId: string, mpsSnapshotId: string): Promise<OpenOrdersResponseDto> {
    return this.get<OpenOrdersResponseDto>(`/api/v1/workspaces/${assignmentId}/open-orders?mpsSnapshotId=${encodeURIComponent(mpsSnapshotId)}`);
  }

  async refreshOpenOrders(assignmentId: string, mpsSnapshotId: string): Promise<OpenOrdersResponseDto> {
    return this.postEmpty<OpenOrdersResponseDto>(`/api/v1/workspaces/${assignmentId}/open-orders/refresh?mpsSnapshotId=${encodeURIComponent(mpsSnapshotId)}`);
  }

  async restoreOpenOrdersDraft(assignmentId: string, mpsSnapshotId: string): Promise<OpenOrdersDraftResponseDto> {
    return this.get<OpenOrdersDraftResponseDto>(`/api/v1/workspaces/${assignmentId}/open-orders/draft?mpsSnapshotId=${encodeURIComponent(mpsSnapshotId)}`);
  }

  async getOpenOrdersDraftPresence(assignmentId: string): Promise<OpenOrdersDraftPresenceDto> {
    return this.get<OpenOrdersDraftPresenceDto>(`/api/v1/workspaces/${assignmentId}/open-orders/draft/presence`);
  }

  async saveOpenOrdersDraft(assignmentId: string, request: SaveOpenOrdersDraftRequestDto): Promise<OpenOrdersDraftResponseDto> {
    return this.put<OpenOrdersDraftResponseDto>(`/api/v1/workspaces/${assignmentId}/open-orders/draft`, request);
  }

  async deleteOpenOrdersDraft(assignmentId: string): Promise<void> {
    return this.delete(`/api/v1/workspaces/${assignmentId}/open-orders/draft`);
  }

  async exportOpenOrdersReport(assignmentId: string, request: ExportOpenOrdersReportRequestDto): Promise<{ blob: Blob; fileName: string | null }> {
    const url = `${this.baseUrl}/api/v1/workspaces/${assignmentId}/open-orders/report-export`;
    const response = await fetch(url, {
      method: 'POST', headers: { 'Content-Type': 'application/json', Accept: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' },
      body: JSON.stringify(request),
    });
    if (!response.ok) { const text = await response.text(); throw new ApiError(response.status, url, text || `HTTP ${response.status} from ${url}`); }
    return { blob: await response.blob(), fileName: response.headers.get('content-disposition')?.match(/filename="?([^";]+)"?/i)?.[1] ?? null };
  }

  async getLongTermShortages(
    assignmentId: string,
    snapshotId: string,
    includeManufacturedParts: boolean,
    includePhantoms: boolean,
    includeUnconfirmed = false,
    horizonWeeks = 26,
    showAll = false,
    includeEvidence = true,
  ): Promise<LongTermShortagesResponseDto> {
    return this.get<LongTermShortagesResponseDto>(
      `/api/v1/workspaces/${assignmentId}/long-term-shortages?snapshotId=${encodeURIComponent(snapshotId)}` +
      `&includeManufacturedParts=${includeManufacturedParts}&includePhantoms=${includePhantoms}&includeUnconfirmed=${includeUnconfirmed}&horizonWeeks=${horizonWeeks}&showAll=${showAll}&includeEvidence=${includeEvidence}`,
    );
  }

  async getLongTermShortagesScreen(assignmentId: string, snapshotId: string,
    includeManufacturedParts: boolean, includePhantoms: boolean, horizonWeeks: number): Promise<components['schemas']['LongTermShortagesScreenDto']> {
    return this.get<components['schemas']['LongTermShortagesScreenDto']>(
      `/api/v1/workspaces/${assignmentId}/long-term-shortages/screen?snapshotId=${encodeURIComponent(snapshotId)}` +
      `&includeManufacturedParts=${includeManufacturedParts}&includePhantoms=${includePhantoms}&horizonWeeks=${horizonWeeks}`);
  }

  async getLongTermShortageProjectionDetail(assignmentId: string, snapshotId: string, componentPart: string,
    includeManufacturedParts: boolean, includePhantoms: boolean, horizonWeeks: number): Promise<components['schemas']['LongTermShortageProjectionDetailDto']> {
    return this.get<components['schemas']['LongTermShortageProjectionDetailDto']>(
      `/api/v1/workspaces/${assignmentId}/long-term-shortages/projection-detail?snapshotId=${encodeURIComponent(snapshotId)}` +
      `&componentPart=${encodeURIComponent(componentPart)}&includeManufacturedParts=${includeManufacturedParts}&includePhantoms=${includePhantoms}&horizonWeeks=${horizonWeeks}`);
  }

  async getLongTermShortagePurchasing(assignmentId: string, snapshotId: string, componentPart: string,
    includeManufacturedParts: boolean, includePhantoms: boolean, horizonWeeks: number): Promise<LongTermShortagePurchasingDto> {
    const query = `?snapshotId=${encodeURIComponent(snapshotId)}&componentPart=${encodeURIComponent(componentPart)}` +
      `&includeManufacturedParts=${includeManufacturedParts}&includePhantoms=${includePhantoms}&horizonWeeks=${horizonWeeks}`;
    return this.get<LongTermShortagePurchasingDto>(`/api/v1/workspaces/${assignmentId}/long-term-shortages/purchasing${query}`);
  }

  async exportLongTermShortages(assignmentId: string, request: ExportLongTermShortagesRequestDto): Promise<{ blob: Blob; fileName: string | null }> {
    const path = `/api/v1/workspaces/${assignmentId}/long-term-shortages/export`;
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'POST', headers: { 'Content-Type': 'application/json', Accept: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }, body: JSON.stringify(request),
    });
    if (!response.ok) { const text = await response.text(); throw new ApiError(response.status, url, text || `HTTP ${response.status} from ${url}`); }
    const disposition = response.headers.get('content-disposition');
    const fileName = disposition?.match(/filename="?([^";]+)"?/i)?.[1] ?? null;
    return { blob: await response.blob(), fileName };
  }

  private async get<T>(path: string): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'GET',
      headers: { Accept: 'application/json' },
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiError(response.status, url, text || `HTTP ${response.status} from ${url}`);
    }

    return response.json() as Promise<T>;
  }

  private async post<T>(path: string, body: unknown): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiError(response.status, url, text || `HTTP ${response.status} from ${url}`);
    }

    return response.json() as Promise<T>;
  }

  private async postEmpty<T>(path: string): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'POST',
      headers: { Accept: 'application/json' },
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiError(response.status, url, text || `HTTP ${response.status} from ${url}`);
    }

    return response.json() as Promise<T>;
  }

  private async put<T>(path: string, body: unknown): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiError(response.status, url, text || `HTTP ${response.status} from ${url}`);
    }

    return response.json() as Promise<T>;
  }

  private async delete(path: string): Promise<void> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'DELETE',
      headers: { Accept: 'application/json' },
    });

    if (!response.ok) {
      const text = await response.text();
      throw new ApiError(response.status, url, text || `HTTP ${response.status} from ${url}`);
    }
  }
}

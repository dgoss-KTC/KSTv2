import type { AnalyticalSaveResult } from '../api/analyticalExportApi';

/** Presentation port only: the implementation retains report-specific native authority. */
export interface PreparedExcel { preparationId: string; fileName: string; warning?: string | null }
export interface EmptyExcel { outcome: 'noData'; message: string }
export interface ExcelSavePort {
  save(prepared: PreparedExcel, openAfterSave: boolean, operationId: string): Promise<AnalyticalSaveResult>;
  status(operationId: string): Promise<AnalyticalSaveResult>;
  open(artifactId: string): Promise<void>;
}
export interface ExcelExportPort<Request> extends ExcelSavePort {
  prepare(request: Request): Promise<PreparedExcel | EmptyExcel>;
  release(): Promise<void>;
}

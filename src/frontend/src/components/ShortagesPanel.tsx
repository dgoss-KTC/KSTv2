import { useEffect, useRef, useState } from 'react';
import type { WorkOrderImmediateMaterialAnalysisResponseDto, WorkOrderImmediateMaterialComponentDto } from '../api/client';
import type { WorkOrdersApiError } from '../api/workOrdersApi';
import { formatOptionalDate, formatQuantity, workOrderStatusLabel } from '../mps/mpsPresentation';
import { useEscapeLevel } from '../mps/escapeStack';
import './ShortagesPanel.css';
import { WorkOrderCandidatePanel } from './WorkOrderCandidatePanel';

const NA = 'N/A';

function materialStatusLabel(status: string): string {
  if (status === 'unknown') return 'Unknown / Data Issue';
  if (status === 'notApplicable') return 'Manufactured Component';
  if (status === 'onHand') return 'On Hand';
  return 'Short';
}

function requirementSourceLabel(source: string): string {
  return source === 'actualWo' ? 'Actual WO Requirement' : 'Projected Requirement';
}

function formatOptionalQuantity(value: number | string | null | undefined): string {
  return value === null || value === undefined ? NA : formatQuantity(value);
}

function formatOptionalPercent(value: number | string | null | undefined): string {
  return value === null || value === undefined ? NA : `${formatQuantity(value)}%`;
}

function sortComponents(rows: readonly WorkOrderImmediateMaterialComponentDto[]) {
  const rank: Record<string, number> = { unknown: 0, short: 1, onHand: 2, notApplicable: 3 };
  return rows
    .map((row, index) => ({ row, index }))
    .sort((a, b) => rank[a.row.materialStatus] - rank[b.row.materialStatus] || a.row.componentPart.localeCompare(b.row.componentPart) || a.index - b.index)
    .map(({ row }) => row);
}

interface ShortagesPanelProps {
  selectedWoid: string | null;
  analysis: WorkOrderImmediateMaterialAnalysisResponseDto | null;
  isLoading: boolean;
  error: WorkOrdersApiError | null;
  onRetry: () => void;
  detail: WorkOrderImmediateMaterialComponentDto | null;
  onSelectDetail: (row: WorkOrderImmediateMaterialComponentDto, element: HTMLElement) => void;
  onCloseDetail: () => void;
  onCloseAnalysis?: () => void;
  registerSelectedWoidEscape?: boolean;
  assignmentId?: string;
  snapshotId?: string | null;
  dateBasis?: string;
}

export function ShortagesPanel({ selectedWoid, analysis, isLoading, error, onRetry, detail, onSelectDetail, onCloseDetail, onCloseAnalysis, registerSelectedWoidEscape = false, assignmentId, snapshotId, dateBasis }: ShortagesPanelProps) {
  const [candidatePart, setCandidatePart] = useState<string | null>(null);
  useEscapeLevel(registerSelectedWoidEscape && selectedWoid !== null, () => onCloseAnalysis?.());
  useEscapeLevel(candidatePart !== null, () => setCandidatePart(null));
  if (!selectedWoid) return <div className="shortages-panel__state">Select a Work Order to inspect its immediate material analysis.</div>;
  if (isLoading) return <div className="shortages-panel__state">Loading immediate material analysis...</div>;
  if (error) {
    return <div className="shortages-panel__state shortages-panel__state--error"><p>{error.type === 'stale' ? 'This schedule context is out of date. Refresh the MPS grid and reselect the Work Order.' : error.detail}</p><button type="button" onClick={onRetry}>Retry</button></div>;
  }
  if (!analysis) return null;
  if (analysis.diagnostic && analysis.components.length === 0) {
    return <div className="shortages-panel__state shortages-panel__state--issue" role="alert"><strong>Unknown / Data Issue</strong><p>{analysis.diagnostic}</p></div>;
  }
  if (analysis.components.length === 0) return <div className="shortages-panel__state">No immediate material components were returned for this Work Order.</div>;
  return <div className="shortages-panel">
    <h3>Immediate Material Analysis - {analysis.workOrder.woid}</h3>
    <div className="shortages-panel__scroll"><table><thead><tr><th>Component</th><th>Description</th><th>BOM Qty</th><th>Issued Qty</th><th>Variance Qty</th><th>Issued %</th><th>Short</th></tr></thead>
      <tbody>{sortComponents(analysis.components).map((row) => <tr key={row.componentPart} className={`shortages-panel__row shortages-panel__row--${row.materialStatus}`}><td className="shortages-panel__component">{row.isManufactured && <button type="button" className="shortages-panel__drill-btn" onClick={() => setCandidatePart((part) => part === row.componentPart ? null : row.componentPart)} aria-label={`Work Orders for ${row.componentPart}`} aria-expanded={candidatePart === row.componentPart}><span className={`shortages-panel__chevron${candidatePart === row.componentPart ? ' shortages-panel__chevron--open' : ''}`} aria-hidden="true" /></button>}<button type="button" onClick={(event) => onSelectDetail(row, event.currentTarget)}>{row.componentPart}</button></td><td>{row.description ?? NA}{row.diagnostic && <span className="shortages-panel__diagnostic"> {row.diagnostic}</span>}</td><td>{formatQuantity(row.requiredQuantity)}</td><td>{formatOptionalQuantity(row.issuedQuantity)}</td><td>{formatOptionalQuantity(row.varianceQuantity)}</td><td>{formatOptionalPercent(row.issuedPercent)}</td><td>{formatOptionalQuantity(row.shortQuantity)}</td></tr>)}</tbody>
    </table></div>
    {candidatePart && assignmentId && dateBasis && <WorkOrderCandidatePanel assignmentId={assignmentId} snapshotId={snapshotId ?? null} immediateParentWoid={selectedWoid} componentPart={candidatePart} depth={2} dateBasis={dateBasis} />}
    {detail && <ShortageDetailModal analysis={analysis} row={detail} onClose={onCloseDetail} />}
  </div>;
}

interface ShortageDetailModalProps { analysis: WorkOrderImmediateMaterialAnalysisResponseDto; row: WorkOrderImmediateMaterialComponentDto; onClose: () => void; }

export function ShortageDetailModal({ analysis, row, onClose }: ShortageDetailModalProps) {
  const closeRef = useRef<HTMLButtonElement>(null);
  useEffect(() => { closeRef.current?.focus(); }, []);
  useEffect(() => {
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => { document.body.style.overflow = previousOverflow; };
  }, []);
  useEffect(() => { const handler = (event: KeyboardEvent) => { if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); onClose(); } }; document.addEventListener('keydown', handler, true); return () => document.removeEventListener('keydown', handler, true); }, [onClose]);
  const incoming = row.incoming;
  const showPo = row.materialStatus === 'short' && incoming?.poNumber != null;
  return <div className="shortage-detail-backdrop"><div className="shortage-detail" role="dialog" aria-modal="true" aria-labelledby="shortage-detail-title"><header><h2 id="shortage-detail-title">Material Detail - {row.componentPart}</h2><button ref={closeRef} type="button" onClick={onClose} aria-label="Close Material Detail">X</button></header><section><h3>Work Order</h3><dl><Field name="WO ID" value={analysis.workOrder.woid}/><Field name="Status" value={workOrderStatusLabel(analysis.workOrder.status)}/><Field name="WO Build Part" value={analysis.workOrder.buildPart}/><Field name="Material-analysis Build Qty" value={formatQuantity(analysis.workOrder.materialBuildQuantity)}/><Field name="Due Date" value={formatOptionalDate(analysis.workOrder.dueDate)}/><Field name="Release Date" value={formatOptionalDate(analysis.workOrder.releaseDate)}/></dl></section><section><h3>Material</h3><dl><Field name="Material Status" value={materialStatusLabel(row.materialStatus)}/><Field name="Requirement Source" value={requirementSourceLabel(row.requirementSource)}/><Field name="Usable On Hand" value={formatQuantity(row.usableOnHand)}/><Field name="Floor Stock / Non-Issued" value={row.isFloorStockOrNonIssued ? 'Yes' : 'No'}/>{row.isOverIssued && <Field name="Over-Issued" value="Yes"/>}</dl>{row.diagnostic && <p className="shortage-detail__diagnostic" role="alert">{row.diagnostic}</p>}</section><section><h3>Inventory Activity</h3><dl><Field name="Transit" value={formatQuantity(row.inventoryActivity.transit)}/><Field name="Inspection" value={formatQuantity(row.inventoryActivity.inspection)}/><Field name="Non-Net" value={formatQuantity(row.inventoryActivity.nonNet)}/><Field name="MRB" value={formatQuantity(row.inventoryActivity.mrb)}/><Field name="NCM Inspection" value={formatQuantity(row.inventoryActivity.ncmInspection)}/><Field name="Expired / Expiring" value={formatQuantity(row.inventoryActivity.expiredExpiring)}/></dl></section>{row.materialStatus === 'short' && incoming && <section><h3>Incoming</h3><dl><Field name="KSS" value={incoming.isKss ? 'Yes' : 'No'}/>{showPo ? <><Field name="PO Number" value={incoming.poNumber!}/><Field name="PO Due" value={formatOptionalDate(incoming.poDueDate)}/><Field name="PO Open Qty" value={formatOptionalQuantity(incoming.poOpenQuantity)}/><Field name="PO Confirmation" value={incoming.poConfirmed === null ? NA : incoming.poConfirmed ? 'Confirmed' : 'Not confirmed'}/><Field name="Tracking Info" value={incoming.trackingInfo ?? NA}/></> : !incoming.isKss && <Field name="PO Number" value="NO PO"/>}</dl></section>}</div></div>;
}

function Field({ name, value }: { name: string; value: string }) { return <div><dt>{name}</dt><dd>{value}</dd></div>; }

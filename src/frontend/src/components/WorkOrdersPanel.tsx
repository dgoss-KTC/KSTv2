import type { WorkOrderImmediateMaterialAnalysisResponseDto, WorkOrderImmediateMaterialComponentDto, WorkOrderImmediateMaterialSummaryResponseDto, WorkOrderSummaryDto } from '../api/client';
import type { WorkOrdersApiError } from '../api/workOrdersApi';
import { WorkOrderCard, type WorkOrderShortageState } from './WorkOrderCard';
import { ShortagesPanel } from './ShortagesPanel';
import './WorkOrdersPanel.css';

interface WorkOrdersPanelProps { parentPart: string; bucketLabel: string; workOrders: WorkOrderSummaryDto[] | null; isLoading: boolean; error: WorkOrdersApiError | null; onRetry: () => void; selectedWoid: string | null; onSelectWoid: (woid: string | null, trigger?: HTMLButtonElement) => void; summary: WorkOrderImmediateMaterialSummaryResponseDto | null; summaryError: WorkOrdersApiError | null; analysis: WorkOrderImmediateMaterialAnalysisResponseDto | null; isAnalysisLoading: boolean; analysisError: WorkOrdersApiError | null; onRetryAnalysis: () => void; detail: WorkOrderImmediateMaterialComponentDto | null; onSelectDetail: (row: WorkOrderImmediateMaterialComponentDto, element: HTMLElement) => void; onCloseDetail: () => void; assignmentId: string; snapshotId: string | null; dateBasis: string; }

export function WorkOrdersPanel(props: WorkOrdersPanelProps) {
  const stateFor = (woid: string): WorkOrderShortageState => {
    if (props.summaryError) return 'unavailable';
    const summaries = props.summary?.workOrders;
    if (!Array.isArray(summaries)) return 'unavailable';
    const item = summaries.find((summary) => summary.woid === woid);
    if (!item) return 'unavailable';
    return item.hasShortage ? 'shortage' : item.hasDataIssue ? 'dataIssue' : 'clear';
  };
  return <div className="work-orders-panel" aria-label={`Work orders for ${props.parentPart}, ${props.bucketLabel}`}><div className="work-orders-panel__header"><h3 className="work-orders-panel__title">Work Orders &mdash; {props.parentPart} &middot; {props.bucketLabel}</h3></div>
    {props.isLoading && <div className="work-orders-panel__state">Loading work orders&hellip;</div>}
    {!props.isLoading && props.error && <div className="work-orders-panel__state work-orders-panel__state--error"><p>{props.error.type === 'stale' ? 'This schedule context is out of date. Refresh the MPS grid and reselect the bucket.' : props.error.detail}</p><button type="button" className="work-orders-panel__retry-btn" onClick={props.onRetry}>Retry</button></div>}
    {!props.isLoading && !props.error && props.workOrders?.length === 0 && <div className="work-orders-panel__state work-orders-panel__state--empty">No work orders in the planning window for this part.</div>}
    {!props.isLoading && !props.error && props.workOrders && props.workOrders.length > 0 && <><ul className="work-orders-panel__list">{props.workOrders.map((wo) => <WorkOrderCard key={wo.woid} workOrder={wo} isOpen={props.selectedWoid === wo.woid} shortageState={stateFor(wo.woid)} onToggle={(trigger) => props.onSelectWoid(props.selectedWoid === wo.woid ? null : wo.woid, trigger)} />)}</ul>
      {props.selectedWoid && <div className="work-orders-panel__detail"><ShortagesPanel selectedWoid={props.selectedWoid} analysis={props.analysis} isLoading={props.isAnalysisLoading} error={props.analysisError} onRetry={props.onRetryAnalysis} detail={props.detail} onSelectDetail={props.onSelectDetail} onCloseDetail={props.onCloseDetail} onCloseAnalysis={() => props.onSelectWoid(null)} registerSelectedWoidEscape assignmentId={props.assignmentId} snapshotId={props.snapshotId} dateBasis={props.dateBasis} /></div>}</>}
  </div>;
}

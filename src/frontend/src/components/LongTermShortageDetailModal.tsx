import { useLayoutEffect, useRef, useState } from 'react';
import type { ComponentOrderLine } from '../api/componentOrdersApi';
import type { LongTermShortagePurchasing, LongTermShortageScreenRow, LongTermShortageProjectionDetail } from '../api/longTermShortagesApi';
import { formatLineConfirmation } from '../componentOrders/componentOrdersPresentation';
import { formatLongTermQuantity } from '../longTermShortages/longTermShortagesPresentation';
import { formatBuyerCommentMetadata } from '../shared/buyerCommentPresentation';
import { FactsStrip, ScrollTable } from '../shared/partInfoParts';

/** Selected-component presentation only: the full projection/evidence remains in the API and workbook. */
interface ShortageInformationProps {
  row: LongTermShortageScreenRow;
  projection: LongTermShortageProjectionDetail | null;
  isProjectionLoading: boolean;
  projectionError: boolean;
  onRetryProjection: () => void;
  asOfDate?: string;
  purchasing: LongTermShortagePurchasing | null;
  isLoading: boolean;
  error: boolean;
  onRetry: () => void;
}

export function ShortageSnapshot({ row, projection, isProjectionLoading, projectionError, onRetryProjection }: Pick<ShortageInformationProps, 'row' | 'projection' | 'isProjectionLoading' | 'projectionError' | 'onRetryProjection'>) {
  const projectedQuantity = (value: number | string | null | undefined) => isProjectionLoading ? '…'
    : projectionError || !projection ? 'Unavailable' : formatLongTermQuantity(value, row.unitOfMeasure);
  return <section className="shortage-info__snapshot" aria-labelledby="shortage-info-snapshot-title">
    <h3 id="shortage-info-snapshot-title">Shortage Snapshot</h3>
    <FactsStrip items={[
      ['First Short Date', row.firstShortDate ?? '—'],
      ['Opening QOH', formatLongTermQuantity(row.openingQoh, row.unitOfMeasure)],
      ['Adjusted Opening QOH', projectedQuantity(projection?.adjustedOpeningQoh)],
      ['Past Gross Requirements', projectedQuantity(projection?.pastGrossRequirements)],
      ['Overdue Receipts (excluded)', projectedQuantity(projection?.overdueReceipts)],
      ['UOM', row.unitOfMeasure ?? 'Unknown'],
    ]} />
    <dl className="long-term-shortage-detail__parents"><dt>Demand Parents</dt><dd>{projection ? projection.demandParentParts.join(', ') || 'No workspace parents' : isProjectionLoading ? 'Loading…' : 'Unavailable'}</dd></dl>
    {row.dataQualityWarning && <p className="long-term-shortage-detail__warning" role="status">{row.dataQualityWarning}</p>}
    {isProjectionLoading ? <p role="status">Loading snapshot projection detail…</p>
      : (projectionError || !projection) && <p role="alert">Snapshot projection detail unavailable. Refresh Workspace Shortages if the snapshot changed. <button type="button" onClick={onRetryProjection}>Retry projection detail</button></p>}
  </section>;
}

function InlineComment({ text }: { text: string }) {
  const [expanded, setExpanded] = useState(false);
  const [clamped, setClamped] = useState(false);
  const paragraph = useRef<HTMLParagraphElement>(null);
  useLayoutEffect(() => {
    const element = paragraph.current;
    if (!element || expanded) return;
    const measure = () => setClamped(element.scrollHeight > element.clientHeight);
    measure();
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(measure);
    observer?.observe(element);
    return () => observer?.disconnect();
  }, [text, expanded]);
  return <>
    <p ref={paragraph} id="shortage-info-comment-text" className={`long-term-shortage-detail__comment${expanded ? '' : ' shortage-info__comment--clamped'}`}>{text}</p>
    {(clamped || text.includes('\n')) && <button type="button" className="shortage-info__comment-toggle" aria-expanded={expanded} aria-controls="shortage-info-comment-text" onClick={() => setExpanded(value => !value)}>{expanded ? 'Show less' : 'Show full comment'}</button>}
  </>;
}

export function ShortageBuyerComment({ purchasing, isLoading, error }: Pick<ShortageInformationProps, 'purchasing' | 'isLoading' | 'error'>) {
  return <section className="part-info__comment-strip"><h3>Current Buyer Comment</h3>
    {isLoading ? <p className="component-info-modal__placeholder">Loading buyer comment…</p>
      : error || !purchasing?.commentAvailable ? <p role="status">Buyer comment unavailable.</p>
        : <>
          {(purchasing.currentComment?.trim() || purchasing.commentRecordedAt) && <p className="buyer-comment-metadata">{formatBuyerCommentMetadata(purchasing.commentRecordedAt, purchasing.commentAuthor)}</p>}
          <InlineComment key={purchasing.currentComment} text={purchasing.currentComment?.trim() ? purchasing.currentComment : purchasing.commentRecordedAt ? 'No comment text in latest entry.' : 'No buyer comment on file'} />
        </>}
  </section>;
}

export function ShortageOpenOrders({ row, asOfDate, purchasing, isLoading, error, onRetry }: Pick<ShortageInformationProps, 'row' | 'asOfDate' | 'purchasing' | 'isLoading' | 'error' | 'onRetry'>) {
  const today = asOfDate ?? localToday();
  const openLines = purchasing?.openPurchaseOrders ?? [];
  // The source is the Stage 10 conventional-open PO path; no additional PO qualification here.
  const lines = [...openLines].sort((a, b) => {
    const rank = (line: ComponentOrderLine) => line.dueDate && line.dueDate < today ? 0 : line.dueDate ? 1 : 2;
    return rank(a) - rank(b) || (a.dueDate ?? '').localeCompare(b.dueDate ?? '') || a.poNumber.localeCompare(b.poNumber) || Number(a.poLine) - Number(b.poLine);
  });
  return <section className="part-info__card"><h3>Open Purchase Orders</h3>
      {isLoading ? <p className="component-info-modal__placeholder">Loading open purchase orders…</p>
        : error ? <p role="alert">Open purchase orders unavailable. <button type="button" onClick={onRetry}>Retry purchase orders</button></p>
          : lines.length === 0 ? <p className="component-info-modal__placeholder">No open purchase orders for this component.</p>
            : <ScrollTable label="Open Purchase Orders" headings={['PO', 'Line', 'Due', 'Open Qty', 'Confirmed', 'Supplier']} numeric={[3]}
              rows={lines.map(line => [line.poNumber, line.poLine,
                <span className={line.dueDate && line.dueDate < today ? 'long-term-shortage-detail__past-due' : undefined}>{line.dueDate ? <>{line.dueDate < today && <span>Past due: </span>}{line.dueDate}</> : 'Due date missing'}</span>,
                formatLongTermQuantity(line.openQuantity, row.unitOfMeasure), formatLineConfirmation(line.confirmed) || 'Unknown', line.supplierDisplay ?? '—'])} />}
  </section>;
}

function localToday(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

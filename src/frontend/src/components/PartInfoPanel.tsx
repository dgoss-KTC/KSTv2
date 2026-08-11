import type { PartDetailResponseDto } from '../api/client';
import type { PartDetailApiError } from '../api/partDetailApi';
import { formatQuantity } from '../mps/mpsPresentation';
import './PartInfoPanel.css';

interface PartInfoPanelProps {
  partNumber: string;
  detail: PartDetailResponseDto | null;
  isLoading: boolean;
  error: PartDetailApiError | null;
  onRetry: () => void;
  onBack: () => void;
}

const NO_VALUE = '\u2014';

function formatOptionalDays(value: number | string | null | undefined): string {
  if (value === null || value === undefined) return NO_VALUE;
  return `${formatQuantity(value)} days`;
}

function formatOptionalQuantity(value: number | string | null | undefined): string {
  if (value === null || value === undefined) return NO_VALUE;
  return formatQuantity(value);
}

function formatStatus(code: string | null | undefined, description: string | null | undefined): string {
  if (!code) return NO_VALUE;
  return description ? `${code} \u2014 ${description}` : code;
}

function formatPrice(value: number | string): string {
  return `$${formatQuantity(value)}`;
}

export function PartInfoPanel({ partNumber, detail, isLoading, error, onRetry, onBack }: PartInfoPanelProps) {
  return (
    <div className="part-info-panel" aria-label={`Part information for ${partNumber}`}>
      <div className="part-info-panel__header">
        <h3 className="part-info-panel__title">Part Info &mdash; {partNumber}</h3>
        <button type="button" className="part-info-panel__back-btn" onClick={onBack}>
          &larr; Back to full grid
        </button>
      </div>

      {isLoading && <div className="part-info-panel__state">Loading part information&hellip;</div>}

      {!isLoading && error?.type === 'missing-part' && (
        <div className="part-info-panel__state part-info-panel__state--missing">
          No QAD part master record was found for {partNumber}.
        </div>
      )}

      {!isLoading && error?.type === 'error' && (
        <div className="part-info-panel__state part-info-panel__state--error">
          <p>{error.detail}</p>
          <button type="button" className="part-info-panel__retry-btn" onClick={onRetry}>
            Retry
          </button>
        </div>
      )}

      {!isLoading && !error && detail && (
        <>
          {detail.isStale && (
            <div className="part-info-panel__banner" role="alert">
              {detail.warning ?? 'Showing the last known part information.'}
            </div>
          )}

          <dl className="part-info-panel__grid">
            <div className="part-info-panel__field part-info-panel__field--wide">
              <dt>Description</dt>
              <dd>{detail.description ?? NO_VALUE}</dd>
            </div>
            <div className="part-info-panel__field">
              <dt>Revision</dt>
              <dd>{detail.currentRevision ?? NO_VALUE}</dd>
            </div>
            <div className="part-info-panel__field">
              <dt>IOS Code</dt>
              <dd>{detail.iosCode ?? NO_VALUE}</dd>
            </div>
            <div className="part-info-panel__field">
              <dt>Part Status</dt>
              <dd>{formatStatus(detail.partStatusCode, detail.partStatusDescription)}</dd>
            </div>
            <div className="part-info-panel__field">
              <dt>Planner</dt>
              <dd>{detail.plannerCode ?? NO_VALUE}</dd>
            </div>
            <div className="part-info-panel__field">
              <dt>Mfg Lead Time</dt>
              <dd>{formatOptionalDays(detail.manufacturingLeadTimeDays)}</dd>
            </div>
            <div className="part-info-panel__field">
              <dt>Safety Time</dt>
              <dd>{formatOptionalDays(detail.safetyTimeDays)}</dd>
            </div>
            <div className="part-info-panel__field">
              <dt>Safety Stock</dt>
              <dd>{formatOptionalQuantity(detail.safetyStockQuantity)}</dd>
            </div>
          </dl>

          <dl className="part-info-panel__grid">
            <div className="part-info-panel__field">
              <dt>Net On Hand</dt>
              <dd>{formatQuantity(detail.quantityOnHand)}</dd>
            </div>
            <div className="part-info-panel__field">
              <dt>Qty Non-Net</dt>
              <dd>{formatQuantity(detail.quantityNonNet)}</dd>
            </div>
            <div className="part-info-panel__field">
              <dt>RMA On Hand</dt>
              <dd>{formatQuantity(detail.quantityRmaOnHand)}</dd>
            </div>
          </dl>

          <div className="part-info-panel__pricing">
            <h4 className="part-info-panel__pricing-title">MOQ / Price</h4>
            {detail.priceBreaks.length === 0 && (
              <p className="part-info-panel__no-data">No Data Found</p>
            )}
            {detail.priceBreaks.length === 1 && (
              <p className="part-info-panel__price-compact">
                {formatQuantity(detail.priceBreaks[0].minimumOrderQuantity)} @ {formatPrice(detail.priceBreaks[0].unitPrice)}
              </p>
            )}
            {detail.priceBreaks.length > 1 && (
              <table className="part-info-panel__price-table">
                <thead>
                  <tr>
                    <th>MOQ</th>
                    <th>Price</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.priceBreaks.map((tier) => (
                    <tr key={`${tier.minimumOrderQuantity}-${tier.unitPrice}`}>
                      <td>{formatQuantity(tier.minimumOrderQuantity)}</td>
                      <td>{formatPrice(tier.unitPrice)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </>
      )}
    </div>
  );
}

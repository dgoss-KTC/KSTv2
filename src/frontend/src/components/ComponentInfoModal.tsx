import { useCallback, useEffect, useRef } from 'react';
import type { ComponentDetailApiError } from '../api/componentDetailApi';
import type { ComponentDetailResponseDto } from '../api/client';
import { formatQuantity } from '../mps/mpsPresentation';
import './ComponentInfoModal.css';

interface ComponentInfoModalProps {
  componentPart: string;
  detail: ComponentDetailResponseDto | null;
  isLoading: boolean;
  error: ComponentDetailApiError | null;
  onRetry: () => void;
  onClose: () => void;
}

const NO_VALUE = '\u2014';

function formatOptionalQuantity(value: number | string | null | undefined): string {
  if (value === null || value === undefined) return NO_VALUE;
  return formatQuantity(value);
}

function formatOptionalDays(value: number | string | null | undefined): string {
  if (value === null || value === undefined) return NO_VALUE;
  return `${formatQuantity(value)} days`;
}

// Distinguishes "no data" (null) from an actual numeric zero — a null Standard Cost/QCTC must
// never render as $0.0000. Always shows exactly four decimal places (owner requirement), unlike
// the general-purpose formatQuantity used elsewhere in this modal.
function formatOptionalPrice(value: number | string | null | undefined): string {
  if (value === null || value === undefined) return NO_VALUE;
  const numeric = Number(value);
  if (Number.isNaN(numeric)) return NO_VALUE;
  return `$${numeric.toFixed(4)}`;
}

function formatStatus(code: string | null | undefined, description: string | null | undefined): string {
  if (!code) return NO_VALUE;
  return description ? `${code} \u2014 ${description}` : code;
}

const FOCUSABLE_SELECTOR =
  'button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * Stage 8D.6 blocking Component Information modal. Opens immediately with the component identity
 * known from the originating BOM row while the detail body loads; the backdrop is blocking-only
 * (no close-on-click) — only the X and Escape close it, restoring focus to the BOM row that
 * opened it (handled by the caller). Component Detail business grain is Site + Component Part
 * only; occurrence/level/qty-per/scrap remain BOM-only concepts and are intentionally not shown
 * here.
 */
export function ComponentInfoModal({
  componentPart,
  detail,
  isLoading,
  error,
  onRetry,
  onClose,
}: ComponentInfoModalProps) {
  const dialogRef = useRef<HTMLDivElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    closeButtonRef.current?.focus();
  }, []);

  useEffect(() => {
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, []);

  // Scoped document-level Escape handler, independent of where focus is inside the modal.
  // Capture phase + stopPropagation so underlying app shortcuts never see this Escape.
  // Registered only while this modal instance is mounted and removed on unmount/re-render.
  useEffect(() => {
    function handleDocumentEscape(e: KeyboardEvent) {
      if (e.key !== 'Escape') return;
      e.preventDefault();
      e.stopPropagation();
      onClose();
    }
    document.addEventListener('keydown', handleDocumentEscape, true);
    return () => document.removeEventListener('keydown', handleDocumentEscape, true);
  }, [onClose]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLDivElement>) => {
      if (e.key !== 'Tab') return;
      const focusable = dialogRef.current?.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR);
      if (!focusable || focusable.length === 0) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    },
    [],
  );

  return (
    <div className="component-info-modal-backdrop">
      <div
        ref={dialogRef}
        className="component-info-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="component-info-modal-title"
        onKeyDown={handleKeyDown}
      >
        <div className="component-info-modal__header">
          <div className="component-info-modal__header-row">
            <h2 id="component-info-modal-title" className="component-info-modal__title">
              Component Information
            </h2>
            <button
              ref={closeButtonRef}
              type="button"
              className="component-info-modal__close-btn"
              onClick={onClose}
              aria-label="Close Component Information"
            >
              &#10005;
            </button>
          </div>
          <div className="component-info-modal__identity-row">
            <div className="component-info-modal__identity">
              <span className="component-info-modal__part">{componentPart}</span>
              <span className="component-info-modal__description">{detail?.description ?? NO_VALUE}</span>
            </div>
            <button
              type="button"
              className="component-info-modal__mrp-btn"
              disabled
              title="Component MRP is a future capability."
            >
              Show MRP
            </button>
          </div>
        </div>

        <div className="component-info-modal__body">
          {isLoading && (
            <div className="component-info-modal__state">Loading component information&hellip;</div>
          )}

          {!isLoading && error && (
            <div className="component-info-modal__state component-info-modal__state--error">
              <p>Component information could not be loaded.</p>
              <p>{error.detail}</p>
              <div className="component-info-modal__error-actions">
                <button type="button" className="component-info-modal__retry-btn" onClick={onRetry}>
                  Retry
                </button>
                <button type="button" className="component-info-modal__close-secondary-btn" onClick={onClose}>
                  Close
                </button>
              </div>
            </div>
          )}

          {!isLoading && !error && detail && (
            <>
              {detail.isStale && (
                <div className="component-info-modal__banner" role="alert">
                  {detail.warning ?? 'Showing the last known component information.'}
                </div>
              )}

              <div className="component-info-modal__upper">
                <div className="component-info-modal__left">
                  <section className="component-info-modal__section">
                    <h3>Inventory</h3>
                    <dl className="component-info-modal__grid">
                      <div className="component-info-modal__field">
                        <dt>Net QOH</dt>
                        <dd>{formatQuantity(detail.netQuantityOnHand)}</dd>
                      </div>
                      <div className="component-info-modal__field">
                        <dt>Non-Net QOH</dt>
                        <dd>{formatQuantity(detail.nonNetQuantityOnHand)}</dd>
                      </div>
                    </dl>
                  </section>

                  <section className="component-info-modal__section">
                    <h3>Cost</h3>
                    <dl className="component-info-modal__grid">
                      <div className="component-info-modal__field">
                        <dt>Standard Cost</dt>
                        <dd>{formatOptionalPrice(detail.standardCost)}</dd>
                      </div>
                      <div className="component-info-modal__field">
                        <dt>QCTC</dt>
                        <dd>{formatOptionalPrice(detail.qctc)}</dd>
                      </div>
                    </dl>
                  </section>

                  <section className="component-info-modal__section">
                    <h3>Planning</h3>
                    <dl className="component-info-modal__grid">
                      <div className="component-info-modal__field">
                        <dt>Time Fence</dt>
                        <dd>{formatOptionalQuantity(detail.timeFence)}</dd>
                      </div>
                      <div className="component-info-modal__field">
                        <dt>Safety Time</dt>
                        <dd>{formatOptionalDays(detail.safetyTime)}</dd>
                      </div>
                      <div className="component-info-modal__field">
                        <dt>Safety Stock</dt>
                        <dd>{formatOptionalQuantity(detail.safetyStock)}</dd>
                      </div>
                      <div className="component-info-modal__field">
                        <dt>Buyer / Planner</dt>
                        <dd>{detail.buyerPlanner ?? NO_VALUE}</dd>
                      </div>
                    </dl>
                  </section>

                  <section className="component-info-modal__section">
                    <h3>Lead Times / Ordering</h3>
                    <dl className="component-info-modal__grid">
                      <div className="component-info-modal__field">
                        <dt>Purchase LT</dt>
                        <dd>{formatOptionalDays(detail.purchaseLeadTimeDays)}</dd>
                      </div>
                      <div className="component-info-modal__field">
                        <dt>Inspect LT</dt>
                        <dd>{formatOptionalDays(detail.inspectionLeadTimeDays)}</dd>
                      </div>
                      <div className="component-info-modal__field">
                        <dt>Cumulative LT</dt>
                        <dd>{formatOptionalDays(detail.cumulativeLeadTimeDays)}</dd>
                      </div>
                      <div className="component-info-modal__field">
                        <dt>Min Order</dt>
                        <dd>{formatOptionalQuantity(detail.minimumOrderQuantity)}</dd>
                      </div>
                      <div className="component-info-modal__field">
                        <dt>Order Multiple</dt>
                        <dd>{formatOptionalQuantity(detail.orderMultiple)}</dd>
                      </div>
                    </dl>
                  </section>

                  <section className="component-info-modal__section">
                    <h3>Reference</h3>
                    <dl className="component-info-modal__grid">
                      <div className="component-info-modal__field">
                        <dt>Part Status</dt>
                        <dd>{formatStatus(detail.partStatusCode, detail.partStatusDescription)}</dd>
                      </div>
                      <div className="component-info-modal__field">
                        <dt>IOS</dt>
                        <dd>{detail.iosCode ?? NO_VALUE}</dd>
                      </div>
                    </dl>
                  </section>
                </div>

                <div className="component-info-modal__right">
                  <section className="component-info-modal__section">
                    <h3>Inventory / Lot Locations</h3>
                    <p className="component-info-modal__placeholder">
                      Inventory location detail will be added in a later stage.
                    </p>
                  </section>
                </div>
              </div>

              <div className="component-info-modal__lower">
                <section className="component-info-modal__section">
                  <h3>Approved Vendors</h3>
                  <p className="component-info-modal__placeholder">
                    Approved vendor detail will be added in a later stage.
                  </p>
                </section>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

import { useCallback, useEffect, useRef, useState } from 'react';
import type { ComponentDetailApiError } from '../api/componentDetailApi';
import type { ApprovedVendorsApiError } from '../api/approvedVendorsApi';
import type { ComponentDetailResponseDto, ApprovedVendorDto } from '../api/client';
import type { LongTermShortagePurchasing, LongTermShortageScreenRow, LongTermShortageProjectionDetail } from '../api/longTermShortagesApi';
import { formatQuantity } from '../mps/mpsPresentation';
import { ShortageSnapshot, ShortageBuyerComment, ShortageOpenOrders } from './LongTermShortageDetailModal';
import { PartIdentity, FactsStrip, InfoCard, Fields, InfoTabs, ScrollTable } from '../shared/partInfoParts';
import { SupplierStrip } from '../shared/SupplierStrip';
import '../shared/shortageSeverity.css';
import './ComponentInfoModal.css';
import { InventoryLocationsPanel } from './InventoryLocationsPanel';

interface ComponentInfoModalProps {
  assignmentId?: string;
  mpsSnapshotId?: string | null;
  workspaceSite?: string | null;
  context?: 'bom' | 'shortages';
  shortage?: LongTermShortageScreenRow;
  projectionDetail?: LongTermShortageProjectionDetail | null;
  isProjectionLoading?: boolean;
  projectionError?: boolean;
  onRetryProjection?: () => void;
  asOfDate?: string;
  purchasing?: LongTermShortagePurchasing | null;
  isPurchasingLoading?: boolean;
  purchasingError?: boolean;
  onRetryPurchasing?: () => void;
  componentPart: string;
  detail: ComponentDetailResponseDto | null;
  isLoading: boolean;
  error: ComponentDetailApiError | null;
  onRetry: () => void;
  onClose: () => void;
  approvedVendors: ApprovedVendorDto[] | null;
  isApprovedVendorsLoading: boolean;
  approvedVendorsError: ApprovedVendorsApiError | null;
  onExpandApprovedVendors: () => void;
  onRetryApprovedVendors: () => void;
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
  'button:not([disabled]):not([tabindex="-1"]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), summary, [tabindex]:not([tabindex="-1"])';

/**
 * Stage 8D.6 blocking Component Information modal. Opens immediately with the component identity
 * known from the originating BOM row while the detail body loads; the backdrop is blocking-only
 * (no close-on-click) — only the X and Escape close it, restoring focus to the BOM row that
 * opened it (handled by the caller). Component Detail business grain is Site + Component Part
 * only; occurrence/level/qty-per/scrap remain BOM-only concepts and are intentionally not shown
 * here.
 */
export function ComponentInfoModal(props: ComponentInfoModalProps) {
  const {
  assignmentId,
  mpsSnapshotId = null,
  workspaceSite = null,
  context = 'bom',
  shortage,
  componentPart,
  detail,
  isLoading,
  error,
  onRetry,
  onClose,
  approvedVendors,
  isApprovedVendorsLoading,
  approvedVendorsError,
  onExpandApprovedVendors,
  onRetryApprovedVendors,
  } = props;
  const dialogRef = useRef<HTMLDivElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  // Disclosure starts collapsed for each newly mounted component modal.
  const [isAvlExpanded, setIsAvlExpanded] = useState(false);

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
      const focusable = [...(dialogRef.current?.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR) ?? [])].filter(element =>
        !element.closest('[hidden], [inert]') && ![...dialogRef.current!.querySelectorAll('details:not([open])')].some(details =>
          details.contains(element) && element !== details.querySelector('summary')));
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

  const handleToggleApprovedVendors = useCallback(() => {
    setIsAvlExpanded((prev) => {
      const next = !prev;
      if (next) onExpandApprovedVendors();
      return next;
    });
  }, [onExpandApprovedVendors]);

  return (
    <div className="component-info-modal-backdrop">
      <div
        ref={dialogRef}
        className={`component-info-modal${context === 'shortages' ? ' component-info-modal--shortages' : ''}`}
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
          {context === 'shortages' ? <PartIdentity part={componentPart} description={detail?.description ?? NO_VALUE} chips={[
            workspaceSite && <span key="site" className="report-chip">Site {workspaceSite}</span>,
            <span key="status" className="report-chip">{detail ? formatStatus(detail.partStatusCode, detail.partStatusDescription) : isLoading ? shortage?.qadStatus ?? NO_VALUE : NO_VALUE}</span>,
            shortage?.isKss && <span key="kss" className="report-chip">KSS</span>,
            shortage && shortage.severity !== 'Healthy' && <span key="severity" className="shortage-severity" data-severity={shortage.severity}>{({ CriticalShort: 'Critical Short', FutureShort: 'Future Short', SafetyStockShort: 'Safety Short', SafetyStockUnavailable: 'SS Unknown' } as Record<string, string>)[shortage.severity] ?? shortage.severity}</span>,
          ]} /> : <div className="component-info-modal__identity-row">
            <div className="component-info-modal__identity">
              <span className="component-info-modal__part">{componentPart}</span>
              <span className="component-info-modal__description">{detail?.description ?? NO_VALUE}</span>
            </div>
            {context === 'bom' && <button
              type="button"
              className="component-info-modal__mrp-btn"
              disabled
              title="Component MRP is a future capability."
            >
              Show MRP
            </button>}
          </div>}
        </div>

        <div className={`component-info-modal__body${context === 'shortages' ? ' component-info-modal__body--shortages' : ''}`}>
          {context === 'shortages' ? <ShortageComponentInformation key={JSON.stringify([assignmentId, workspaceSite, componentPart])} {...props} /> : <div className="component-info-modal__base">
          {isLoading && context === 'bom' && (
            <div className="component-info-modal__state">Loading component information&hellip;</div>
          )}

          {!isLoading && error && context === 'bom' && (
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

          {(
            <>
              {detail?.isStale && (
                <div className="component-info-modal__banner" role="alert">
                  {detail.warning ?? 'Showing the last known component information.'}
                </div>
              )}

              <div className="component-info-modal__columns">
                <div className="component-info-modal__left" aria-label="Component information">
                  {detail ? <>
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
                  </> : null}
                  <section className="component-info-modal__section component-info-modal__alternates">
                    <button type="button" className="component-info-modal__avl-toggle" aria-expanded={isAvlExpanded}
                      aria-controls="component-info-modal-avl-content" onClick={handleToggleApprovedVendors}>
                      <span className="component-info-modal__avl-disclosure" aria-hidden="true">{isAvlExpanded ? '\u25BE' : '\u25B8'}</span>
                      Approved Alternates
                    </button>
                    {isAvlExpanded && <div id="component-info-modal-avl-content" className="component-info-modal__avl-content">
                      {isApprovedVendorsLoading && <p className="component-info-modal__placeholder">Loading approved alternates&hellip;</p>}
                      {!isApprovedVendorsLoading && approvedVendorsError && <div className="component-info-modal__avl-error"><p>Approved alternates could not be loaded.</p><button type="button" className="component-info-modal__retry-btn" onClick={onRetryApprovedVendors}>Retry</button></div>}
                      {!isApprovedVendorsLoading && !approvedVendorsError && approvedVendors && (approvedVendors.length === 0
                        ? <p className="component-info-modal__placeholder">No approved alternates found.</p>
                        : <div className="component-info-modal__avl-scroll"><table className="component-info-modal__avl-table"><thead><tr><th>Supplier</th><th>Vendor Name</th><th>Supplier Item</th><th>MFG Part</th></tr></thead><tbody>
                          {approvedVendors.map((vendor, index) => <tr key={`${vendor.supplier}-${index}`}><td>{vendor.supplier}</td><td>{vendor.vendorName ?? NO_VALUE}</td><td>{vendor.supplierItem ?? NO_VALUE}</td><td>{vendor.manufacturerPart ?? NO_VALUE}</td></tr>)}
                        </tbody></table></div>)}
                    </div>}
                  </section>
                </div>

                <div className="component-info-modal__right" aria-label="Inventory / Lot Locations column">
                  <InventoryLocationsPanel assignmentId={assignmentId ?? null} componentPart={componentPart}
                    snapshotId={mpsSnapshotId} site={workspaceSite} />
                </div>
              </div>
            </>
          )}
          </div>}
        </div>
      </div>
    </div>
  );
}

/** Shortages presentation only. Keyed by the selected identity, never by the active table. */
function ShortageComponentInformation({ assignmentId, mpsSnapshotId = null, workspaceSite = null, componentPart,
  detail, isLoading, error, onRetry, shortage, projectionDetail = null, isProjectionLoading = false,
  projectionError = false, onRetryProjection = () => {}, asOfDate, purchasing = null, isPurchasingLoading = false,
  purchasingError = false, onRetryPurchasing = () => {}, approvedVendors, isApprovedVendorsLoading,
  approvedVendorsError, onExpandApprovedVendors, onRetryApprovedVendors,
}: ComponentInfoModalProps) {
  const [tab, setTab] = useState<'orders' | 'locations' | 'alternates'>('orders');
  const activated = useRef(false);
  function changeTab(next: typeof tab) {
    setTab(next);
    if (next === 'alternates' && !activated.current) {
      activated.current = true;
      onExpandApprovedVendors();
    }
  }
  return <div className="part-info part-info--embedded">
    {detail?.isStale && <div className="component-info-modal__banner" role="alert">{detail.warning ?? 'Showing the last known component information.'}</div>}
    {shortage && <ShortageSnapshot row={shortage} projection={projectionDetail} isProjectionLoading={isProjectionLoading} projectionError={projectionError} onRetryProjection={onRetryProjection} />}
    {isLoading ? <p className="component-info-modal__placeholder">Loading component master information…</p>
      : error ? <p role="status">Component master information unavailable. <button type="button" onClick={onRetry}>Retry component information</button></p>
        : !detail ? <p className="component-info-modal__placeholder">Component master information is unavailable.</p> : null}
    {detail && !isLoading && !error && <FactsStrip label="Component master facts" items={[
      ['Net QOH', formatQuantity(detail.netQuantityOnHand)],
      ['Std / QCTC', <><span>{formatOptionalPrice(detail.standardCost)}</span> / <span>{formatOptionalPrice(detail.qctc)}</span></>],
      ['Purchase LT', formatOptionalDays(detail.purchaseLeadTimeDays)],
      ['Min / multiple', `${formatOptionalQuantity(detail.minimumOrderQuantity)} / ${formatOptionalQuantity(detail.orderMultiple)}`],
      ['Safety time', formatOptionalDays(detail.safetyTime)],
      ['Buyer / IOS', `${detail.buyerPlanner ?? NO_VALUE} / ${detail.iosCode ?? NO_VALUE}`],
    ]} />}
    <details className="part-info__more"><summary>More part details</summary><div className="part-info__details-grid">
      {detail && <>
        <InfoCard title="Inventory / Planning"><Fields values={[
          ['Non-Net QOH', formatQuantity(detail.nonNetQuantityOnHand)], ['Time Fence', formatOptionalQuantity(detail.timeFence)], ['Safety Stock', formatOptionalQuantity(detail.safetyStock)],
        ]} /></InfoCard>
        <InfoCard title="Additional lead times"><Fields values={[
          ['Inspect LT', formatOptionalDays(detail.inspectionLeadTimeDays)], ['Cumulative LT', formatOptionalDays(detail.cumulativeLeadTimeDays)],
        ]} /></InfoCard>
      </>}
      <InfoCard title="Manufacturer item"><Fields values={[
        ['Manufacturer Item', projectionDetail?.manufacturerItem ?? (isProjectionLoading ? 'Loading…' : projectionError ? 'Unavailable' : NO_VALUE)],
        ...(projectionDetail?.effectivePmCode === 'M' && projectionDetail.manufacturingLeadWorkingDays !== null
          ? [['Manufacturing Lead (site working days)', projectionDetail.manufacturingLeadWorkingDays] as [string, React.ReactNode]] : []),
      ]} /></InfoCard>
     </div></details>
     <SupplierStrip section={purchasing?.suppliers ?? { value: null, acquiredAtUtc: null,
       error: isPurchasingLoading ? null : purchasingError ? 'Supplier status unavailable because open purchase orders could not be acquired.' : 'Supplier status is unavailable.' }}
       isLoading={isPurchasingLoading} missingIdentity={purchasing?.supplierIdentityUnavailable} unavailableDetails
       note="Current supplier statuses for this selected component’s qualifying open POs."
       actions={!isPurchasingLoading && (purchasingError || !!purchasing?.suppliers?.error) ? <button type="button" onClick={onRetryPurchasing}>Retry supplier status</button> : undefined} />
     <ShortageBuyerComment purchasing={purchasing} isLoading={isPurchasingLoading} error={purchasingError} />
    <InfoTabs idPrefix="shortage-info" label="Component information tables" active={tab} onChange={changeTab} tabs={[
      { id: 'orders', label: 'Open POs', count: isPurchasingLoading ? undefined : purchasingError ? 'Unavailable' : purchasing?.openPurchaseOrders.length, failed: purchasingError },
      { id: 'locations', label: 'Inventory lots' },
      { id: 'alternates', label: 'Approved alternates', count: isApprovedVendorsLoading ? undefined : approvedVendorsError ? 'Unavailable' : approvedVendors?.length, failed: !!approvedVendorsError },
    ]} />
    <div id="shortage-info-panel-orders" role="tabpanel" aria-labelledby="shortage-info-tab-orders" hidden={tab !== 'orders'}>
      {shortage && <ShortageOpenOrders row={shortage} asOfDate={asOfDate} purchasing={purchasing} isLoading={isPurchasingLoading} error={purchasingError} onRetry={onRetryPurchasing} />}
    </div>
    <div id="shortage-info-panel-locations" role="tabpanel" aria-labelledby="shortage-info-tab-locations" hidden={tab !== 'locations'}>
      <InventoryLocationsPanel assignmentId={assignmentId ?? null} componentPart={componentPart} snapshotId={mpsSnapshotId} site={workspaceSite} />
    </div>
    <div id="shortage-info-panel-alternates" role="tabpanel" aria-labelledby="shortage-info-tab-alternates" hidden={tab !== 'alternates'}>
      <InfoCard title="Approved Alternates">
        {isApprovedVendorsLoading && <p className="component-info-modal__placeholder">Loading approved alternates…</p>}
        {!isApprovedVendorsLoading && approvedVendorsError && <div className="component-info-modal__avl-error"><p>Approved alternates could not be loaded.</p><button type="button" className="component-info-modal__retry-btn" onClick={onRetryApprovedVendors}>Retry</button></div>}
        {!isApprovedVendorsLoading && !approvedVendorsError && approvedVendors && (approvedVendors.length === 0
          ? <p className="component-info-modal__placeholder">No approved alternates found.</p>
          : <ScrollTable label="Approved Alternates" headings={['Supplier', 'Vendor Name', 'Supplier Item', 'MFG Part']} rows={approvedVendors.map(vendor => [vendor.supplier, vendor.vendorName, vendor.supplierItem, vendor.manufacturerPart])} />)}
      </InfoCard>
    </div>
  </div>;
}

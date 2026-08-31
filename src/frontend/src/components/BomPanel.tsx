import { useId, useMemo, useState } from 'react';
import type { BomLineDto, BomResponseDto } from '../api/client';
import type { BomApiError } from '../api/bomApi';
import { filterMaterialLinesByPart, formatQuantity } from '../mps/mpsPresentation';
import './BomPanel.css';

interface BomPanelProps {
  parentPart: string;
  bom: BomResponseDto | null;
  isLoading: boolean;
  error: BomApiError | null;
  onRetry: () => void;
  /**
   * Opens the Stage 8D.6 Component Information modal for a row's component part. Component
   * Detail's business grain is Site + Component Part only, so `occurrenceKey`/Level are not
   * passed — the originating row element is passed so focus can be restored on close.
   */
  onSelectComponent: (componentPart: string, rowElement: HTMLElement) => void;
}

const NO_VALUE = '\u2014';

/**
 * Display-only filters for the accepted BOM rows. They never re-query, never sort, never
 * regroup, and never change structure/effectivity/inventory semantics — they only hide rows
 * that do not match the scheduler's current view (Stage 8D.4 final filter correction).
 */
type PmFilter = 'all' | 'P' | 'M';
type PhantomFilter = 'all' | 'yes' | 'no';

function formatOptionalQuantity(value: number | string | null | undefined): string {
  if (value === null || value === undefined) return NO_VALUE;
  return formatQuantity(value);
}

function formatScrap(value: number | string | null | undefined): string {
  if (value === null || value === undefined) return NO_VALUE;
  return `${formatQuantity(value)}%`;
}

/**
 * Stage 8D.4 BOM tab: the scheduler-visible multi-level BOM for the selected parent, exactly as
 * returned by the accepted 8D.3 endpoint. Rows preserve backend structural order (no sort, no
 * regroup, no dedup), show actual Level values (gaps included), and are keyed by the opaque
 * OccurrenceKey. Search is a client-side Component Part substring filter (the accepted Stage 7
 * kitting search interaction) plus a separate client-side Description substring filter; neither
 * re-queries. Any row (Stage 8D.6) opens the blocking
 * Component Information modal for its component part via `onSelectComponent`; this panel owns no
 * modal state itself.
 */
export function BomPanel({ parentPart, bom, isLoading, error, onRetry, onSelectComponent }: BomPanelProps) {
  const [searchText, setSearchText] = useState('');
  const [descriptionText, setDescriptionText] = useState('');
  const [pmFilter, setPmFilter] = useState<PmFilter>('all');
  const [phantomFilter, setPhantomFilter] = useState<PhantomFilter>('all');
  const searchInputId = useId();
  const descriptionInputId = useId();
  const pmSelectId = useId();
  const phantomSelectId = useId();

  const filtersActive =
    searchText.length > 0 || descriptionText.length > 0 || pmFilter !== 'all' || phantomFilter !== 'all';

  const clearFilters = () => {
    setSearchText('');
    setDescriptionText('');
    setPmFilter('all');
    setPhantomFilter('all');
  };

  // The accepted kitting Component Item search (trim + case-insensitive substring on
  // componentPart only), the separate Description filter (trim + case-insensitive substring on
  // the displayed description; null/blank descriptions never match a non-empty query), and the
  // P/M and Phantom display filters, combined using AND semantics.
  // Original API order, Level values, gaps, and repeated occurrences are preserved.
  const visibleLines: BomLineDto[] = useMemo(() => {
    if (!bom) return [];
    const descriptionNeedle = descriptionText.trim().toLowerCase();
    return filterMaterialLinesByPart(bom.lines, searchText).filter((line) => {
      if (descriptionNeedle && !(line.description ?? '').toLowerCase().includes(descriptionNeedle)) {
        return false;
      }
      if (pmFilter !== 'all' && line.pmCode !== pmFilter) return false;
      if (phantomFilter === 'yes' && !line.isPhantom) return false;
      if (phantomFilter === 'no' && line.isPhantom) return false;
      return true;
    });
  }, [bom, searchText, descriptionText, pmFilter, phantomFilter]);

  return (
    <div className="bom-panel" aria-label={`BOM for ${parentPart}`}>
      <div className="bom-panel__header">
        <h3 className="bom-panel__title">BOM &mdash; {parentPart}</h3>
      </div>

      {isLoading && <div className="bom-panel__state">Loading BOM&hellip;</div>}

      {!isLoading && error && (
        <div className="bom-panel__state bom-panel__state--error">
          <p>{error.detail}</p>
          <button type="button" className="bom-panel__retry-btn" onClick={onRetry}>
            Retry
          </button>
        </div>
      )}

      {!isLoading && !error && bom && (
        <>
          {bom.isStale && (
            <div className="bom-panel__banner" role="alert">
              {bom.warning ?? 'Showing the last known BOM information.'}
            </div>
          )}

          {bom.lines.length === 0 ? (
            <div className="bom-panel__state bom-panel__state--empty">
              No BOM components found for {parentPart}.
            </div>
          ) : (
            <>
              <div className="bom-panel__filter">
                <label htmlFor={searchInputId}>Filter by Component Item</label>
                <input
                  id={searchInputId}
                  type="text"
                  value={searchText}
                  onChange={(event) => setSearchText(event.target.value)}
                  placeholder="e.g. 00-0001"
                />
                <label htmlFor={descriptionInputId}>Filter by Description</label>
                <input
                  id={descriptionInputId}
                  type="text"
                  value={descriptionText}
                  onChange={(event) => setDescriptionText(event.target.value)}
                  placeholder="e.g. PCB"
                />
                <label htmlFor={pmSelectId}>P/M</label>
                <select
                  id={pmSelectId}
                  className="bom-panel__select"
                  value={pmFilter}
                  onChange={(event) => setPmFilter(event.target.value as PmFilter)}
                >
                  <option value="all">All</option>
                  <option value="P">P</option>
                  <option value="M">M</option>
                </select>
                <label htmlFor={phantomSelectId}>Phantom</label>
                <select
                  id={phantomSelectId}
                  className="bom-panel__select"
                  value={phantomFilter}
                  onChange={(event) => setPhantomFilter(event.target.value as PhantomFilter)}
                >
                  <option value="all">All</option>
                  <option value="yes">Yes</option>
                  <option value="no">No</option>
                </select>
                {filtersActive && (
                  <button
                    type="button"
                    className="bom-panel__clear-btn"
                    onClick={clearFilters}
                  >
                    Clear
                  </button>
                )}
              </div>

              {visibleLines.length === 0 ? (
                <div className="bom-panel__empty">No BOM components match the current filters.</div>
              ) : (
                <table className="bom-panel__table">
                  <thead>
                    <tr>
                      <th scope="col">Level</th>
                      <th scope="col">Component Item</th>
                      <th scope="col">P/M</th>
                      <th scope="col">Phantom</th>
                      <th scope="col">Description</th>
                      <th scope="col">Qty Per</th>
                      <th scope="col">Scrap</th>
                      <th scope="col">Net QOH</th>
                      <th scope="col">Non-Net QOH</th>
                    </tr>
                  </thead>
                  <tbody>
                    {visibleLines.map((line) => (
                      <tr
                        key={line.occurrenceKey}
                        className="bom-panel__row"
                        tabIndex={0}
                        onClick={(e) => onSelectComponent(line.componentPart, e.currentTarget)}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter' || e.key === ' ') {
                            e.preventDefault();
                            onSelectComponent(line.componentPart, e.currentTarget);
                          }
                        }}
                      >
                        <td>{formatQuantity(line.level)}</td>
                        <td className="bom-panel__component">{line.componentPart}</td>
                        <td>{line.pmCode ?? NO_VALUE}</td>
                        <td>{line.isPhantom ? 'Yes' : 'No'}</td>
                        <td>{line.description ?? NO_VALUE}</td>
                        <td>{formatOptionalQuantity(line.quantityPer)}</td>
                        <td>{formatScrap(line.scrapPercentage)}</td>
                        <td>{formatQuantity(line.netQuantityOnHand)}</td>
                        <td>{formatQuantity(line.nonNetQuantityOnHand)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </>
          )}
        </>
      )}
    </div>
  );
}

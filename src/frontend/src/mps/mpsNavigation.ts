import type { WorkOrderImmediateMaterialComponentDto, WorkOrderSummaryDto } from '../api/client';
import { describeBucketSelection, type BucketSelection } from './mpsPresentation';

export type MpsNavigationLevel = (
  | { kind: 'part'; part: string }
  | { kind: 'bucket'; selection: BucketSelection; orders: WorkOrderSummaryDto[] }
  | { kind: 'material'; woid: string }
  | { kind: 'candidates'; originWoid: string; row: WorkOrderImmediateMaterialComponentDto; depth: number }
) & { trigger: HTMLElement | null; scroll?: { element: HTMLElement; left: number; top: number }[] };

export function captureNavigationScroll(trigger: HTMLElement | null) {
  const positions: { element: HTMLElement; left: number; top: number }[] = [];
  for (let element = trigger; element; element = element.parentElement) {
    if (element.scrollWidth > element.clientWidth || element.scrollHeight > element.clientHeight) {
      positions.push({ element, left: element.scrollLeft, top: element.scrollTop });
    }
  }
  return positions;
}

export function restoreNavigationScroll(positions: MpsNavigationLevel['scroll']) {
  positions?.forEach(({ element, left, top }) => {
    if (element.isConnected) { element.scrollLeft = left; element.scrollTop = top; }
  });
}

export function navigationLabel(level: MpsNavigationLevel): string {
  switch (level.kind) {
    case 'part': return level.part;
    case 'bucket': return describeBucketSelection(level.selection);
    case 'material': return `WO ${level.woid}`;
    case 'candidates': return `${level.row.componentPart} work orders`;
  }
}

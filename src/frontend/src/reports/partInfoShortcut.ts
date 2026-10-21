/** One shell-owned navigation request, not a report/business contract. */
export interface PartInfoShortcut {
  token: number;
  part: string;
  site: string;
  /** Consumption survives lookup remounts and Reports selector changes. */
  claim: () => boolean;
}

/** Only one originating investigation is retained, and only for this Reports detour. */
export interface MpsReportOrigin {
  assignmentId: string;
  snapshotId: string;
  part: string;
  site: string;
  isCurrent: () => boolean;
  depart: () => void;
  restore: () => void;
}

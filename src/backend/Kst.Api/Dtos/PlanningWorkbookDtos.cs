namespace Kst.Api.Dtos;

// Quantities, prices and dollars are invariant decimal strings: JavaScript numbers cannot preserve
// all .NET decimal precision. A successful lookup with no eligible list price uses a $0 price
// (owner-approved amendment 2026-10-07; IsPriced=false marks it), so dollars are "0". Price failures are errors.

public sealed record PlanningPriceTierDto(string MinimumOrderQuantity, string UnitPrice);

public sealed record PlanningBucketDto(
    string Kind, DateOnly? WeekLabel,
    string SalesOrderQuantity, string SalesOrderDollars,
    string Mps, string MpsDollars,
    string Forecast, string EstimatedOnHand);

public sealed record PlanningUndatedDto(string SalesOrderQuantity, string SalesOrderDollars, int LineCount);

public sealed record PlanningPartBlockDto(
    string Part, string? Description, string NetQuantityOnHand,
    IReadOnlyList<PlanningPriceTierDto> PriceTiers, string SelectedPrice, bool IsPriced,
    PlanningBucketDto Falldown, IReadOnlyList<PlanningBucketDto> Weeks, PlanningUndatedDto Undated);

public sealed record PlanningWorkbookResponseDto(
    Guid WorkspaceId, string Site,
    string WorkbookSnapshotId, string MpsSnapshotId, string OpenOrdersSnapshotId,
    DateTimeOffset AcquiredAtUtc, DateOnly PriceEffectiveDate, string PriceSelectionPolicy,
    DateOnly Today, int HorizonWeeks, bool IsStale, string? Warning,
    IReadOnlyList<DateOnly> WeekLabels, IReadOnlyList<PlanningPartBlockDto> Parts,
    int MinimumSelectableHorizonWeeks, int MaximumHorizonWeeks);

// Stage 14.5 saved drafts. Adjustments are signed whole-number text; MPS values are exact decimal
// strings. Null MPS values mean the baseline is unavailable (distinct from "0").

public sealed record PlanningWorkbookDraftPresenceDto(bool Exists);

public sealed record PlanningDraftEntryRequestDto(string Part, DateOnly WeekLabel, string? Adjustment);

public sealed record SavePlanningWorkbookDraftRequestDto(Guid MpsSnapshotId, IReadOnlyList<PlanningDraftEntryRequestDto> Entries);

public sealed record PlanningDraftRowDto(
    string Part, DateOnly WeekLabel, string Adjustment,
    string? SavedMpsQuantity, string? CurrentMps, bool BaselineChanged, string? ProposedMps, string? IssueCode);

public sealed record PlanningWorkbookDraftResponseDto(
    bool Exists, bool IsCorrupt, string? Warning,
    DateTimeOffset? SavedAtUtc, string? SavedMpsSnapshotId, string? SavedSite,
    IReadOnlyList<PlanningDraftRowDto> Rows);

// Stage 14.6 QAD Mass Updater export. Adjustments are signed whole-number text exactly as staged. A
// null WeekLabel targets Falldown, which is not adjustable and rejects the whole export. The file is
// a human-reviewable proposal; KST never submits or imports it.

public sealed record PlanningMassUpdaterEntryRequestDto(string Part, DateOnly? WeekLabel, string? Adjustment);

public sealed record ExportPlanningMassUpdaterRequestDto(
    Guid MpsSnapshotId, string Site, IReadOnlyList<PlanningMassUpdaterEntryRequestDto> Entries);

public sealed record PlanningMassUpdaterExportResponseDto(
    string FileName, string ContentBase64, int RowCount, string MpsSnapshotId, string Site);

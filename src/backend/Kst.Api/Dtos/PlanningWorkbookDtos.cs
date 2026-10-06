namespace Kst.Api.Dtos;

// Quantities, prices and dollars are invariant decimal strings: JavaScript numbers cannot preserve
// all .NET decimal precision. Dollar fields are null when the part is unpriced; a valid zero price
// yields "0" dollars.

public sealed record PlanningPriceTierDto(string MinimumOrderQuantity, string UnitPrice);

public sealed record PlanningBucketDto(
    string Kind, DateOnly? WeekLabel,
    string SalesOrderQuantity, string SalesOrderDollars,
    string Mps, string? MpsDollars,
    string Forecast, string EstimatedOnHand);

public sealed record PlanningUndatedDto(string SalesOrderQuantity, string SalesOrderDollars, int LineCount);

public sealed record PlanningPartBlockDto(
    string Part, string? Description, string NetQuantityOnHand,
    IReadOnlyList<PlanningPriceTierDto> PriceTiers, string? SelectedPrice, bool IsPriced,
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

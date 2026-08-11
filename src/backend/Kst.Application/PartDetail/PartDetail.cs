namespace Kst.Application.PartDetail;

/// <summary>
/// Final composed Stage 6 PartDetail, including cache/freshness metadata. Kept in Kst.Application
/// (not Kst.Domain) because <see cref="LoadedAtUtc"/>/<see cref="IsStale"/>/<see cref="Warning"/> are
/// orchestration/cache concerns, matching how <c>MpsSnapshot</c>/<c>MpsWorkspaceState</c> (not
/// <c>MpsPartSchedule</c>) live in Kst.Application.Mps despite also being "the data model".
/// </summary>
public sealed record PartDetail(
    string Site,
    string PartNumber,
    string? PlannerCode,
    decimal? ManufacturingLeadTimeDays,
    decimal? SafetyTimeDays,
    string? PartStatusCode,
    string? PartStatusDescription,
    string? CurrentRevision,
    string? Description,
    string? IosCode,
    decimal? SafetyStockQuantity,
    decimal QuantityOnHand,
    decimal QuantityNonNet,
    decimal QuantityRmaOnHand,
    IReadOnlyList<Kst.Domain.PartDetail.PartPriceBreak> PriceBreaks,
    DateTimeOffset LoadedAtUtc,
    bool IsStale,
    string? Warning
);

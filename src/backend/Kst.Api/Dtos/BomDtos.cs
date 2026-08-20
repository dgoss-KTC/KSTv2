namespace Kst.Api.Dtos;

/// <summary>
/// One scheduler-visible BOM line: a structural occurrence (actual level, opaque occurrence
/// identity) composed with shared Site + Part inventory. Deliberately has no RMA field and no
/// requirement/coverage fields — Stage 8 BOM is informational.
/// </summary>
public sealed record BomLineDto(
    string OccurrenceKey,
    int Level,
    string ComponentPart,
    string? PmCode,
    bool IsPhantom,
    string? Description,
    decimal? QuantityPer,
    decimal? ScrapPercentage,
    decimal NetQuantityOnHand,
    decimal NonNetQuantityOnHand);

/// <summary>
/// Scheduler-visible current-effective BOM for one workspace parent part. <see cref="EffectiveDate"/>
/// is the effective date actually used (backend clock; the caller never supplies it). An empty
/// <see cref="Lines"/> array is a valid successful result. <see cref="IsStale"/>/<see cref="Warning"/>
/// report same-site/same-effective-date stale last-good service after a failed load.
/// </summary>
public sealed record BomResponseDto(
    string Site,
    string ParentPart,
    DateOnly EffectiveDate,
    IReadOnlyList<BomLineDto> Lines,
    DateTimeOffset LoadedAtUtc,
    bool IsStale,
    string? Warning);

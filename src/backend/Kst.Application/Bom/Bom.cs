namespace Kst.Application.Bom;

/// <summary>
/// The complete successfully-composed scheduler-visible BOM for one parent at one site and one
/// effective date, including cache/freshness metadata. Kept in Kst.Application (not Kst.Domain)
/// because <see cref="LoadedAtUtc"/>/<see cref="IsStale"/>/<see cref="Warning"/> are
/// orchestration/cache concerns, matching how <c>PartDetail</c> lives in
/// <c>Kst.Application.PartDetail</c>.
///
/// Business identity is Site + ParentPart + EffectiveDate. <see cref="EffectiveDate"/> is the
/// effective date actually used for the structural read and is reported in the API response.
/// An empty <see cref="Lines"/> collection is a valid successful result (a valid in-scope
/// parent with no effective structural rows, or no scheduler-visible P/M rows), never an error.
/// </summary>
public sealed record Bom(
    string Site,
    string ParentPart,
    DateOnly EffectiveDate,
    IReadOnlyList<BomLine> Lines,
    DateTimeOffset LoadedAtUtc,
    bool IsStale,
    string? Warning);

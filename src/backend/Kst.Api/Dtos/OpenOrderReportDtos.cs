namespace Kst.Api.Dtos;

/// <summary>Independent Python-compatible filters. Blank site means all six supported pairs.
/// Dates are input text so shortened dates and separator variants are validated by the backend.</summary>
public sealed record OpenOrderReportRequestDto(
    string? CustomerName, string? CustomerNumber, string? SalespersonId,
    string? ProductLineFrom, string? ProductLineTo, string? Ios,
    string? DueDateFrom, string? DueDateTo, string? Site,
    IReadOnlyList<string> OptionalFields);

/// <summary>NoData has no preparation/file; prepared bytes are immutable and owner/kind-bound.</summary>
public sealed record OpenOrderReportPreparedDto(string Outcome, string? PreparationId, string? FileName,
    DateOnly AsOfDate, DateTimeOffset AcquiredAtUtc, DateTimeOffset? ExpiresAtUtc);

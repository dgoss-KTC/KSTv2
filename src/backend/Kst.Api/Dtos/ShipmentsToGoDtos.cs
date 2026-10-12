namespace Kst.Api.Dtos;

/// <summary>Independent report selection; domains and clocks are source-owned.</summary>
public sealed record ShipmentsToGoRequestDto(IReadOnlyList<string> Sites, bool IncludeNextWeek = false);

/// <summary>NoData has no preparation or file. Prepared bytes are immutable and session-owned.</summary>
public sealed record ShipmentsToGoPreparedDto(string Outcome, string? PreparationId, string? FileName,
    DateOnly AsOfDate, DateTimeOffset AcquiredAtUtc, DateTimeOffset? ExpiresAtUtc);

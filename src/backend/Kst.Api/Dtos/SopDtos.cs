using System.Text.Json.Serialization;

namespace Kst.Api.Dtos;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SopRangeDto(string From, string To);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SopRequestDto(string Site, string GroupBy, IReadOnlyList<SopRangeDto> Ranges);
public sealed record SopAcquiredDto(string Outcome, string? AcquisitionId, IReadOnlyList<DateOnly> WeekLabels,
    DateOnly AsOfDate, DateTimeOffset AcquiredAtUtc, DateTimeOffset? ExpiresAtUtc);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SopPeriodDto(int FiscalYear, int Period, int Quarter, DateOnly StartSunday,
    int WeekCount, IReadOnlyList<DateOnly> WeekLabels);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SopCalendarExceptionDto(int FiscalYear, int ExtraWeekPeriod);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SopPresentationDto(int AnchorFiscalYear, DateOnly AnchorStartDate,
    IReadOnlyList<SopCalendarExceptionDto> Exceptions, IReadOnlyList<SopPeriodDto> Periods);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SopPrepareDto(string AcquisitionId, SopPresentationDto Presentation);
public sealed record SopPreparedDto(string Outcome, string? PreparationId, string? FileName,
    DateOnly AsOfDate, DateTimeOffset AcquiredAtUtc, DateTimeOffset? ExpiresAtUtc);

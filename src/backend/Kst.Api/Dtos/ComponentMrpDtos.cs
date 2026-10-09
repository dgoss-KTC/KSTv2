namespace Kst.Api.Dtos;

public sealed record ComponentMrpRequestDto(string Site, string Part, int HorizonWeeks = 26, bool IncludeUnconfirmed = true, string Period = "Week");
public sealed record ComponentMrpPreparedDto(string PreparationId, string FileName, DateOnly AsOfDate,
    DateTimeOffset AcquiredAtUtc, DateTimeOffset ExpiresAtUtc, string? Warning, string Period, bool IncludeUnconfirmed, DateOnly ForecastEndExclusive);

namespace Kst.Api.Dtos;

public sealed record ComponentMrpRequestDto(string Site, string Part, int HorizonWeeks = 26, bool IncludeUnconfirmed = true, string Period = "Week",
    string? AcquisitionId = null, DateOnly? AsOfDate = null);
public sealed record ComponentMrpPeriodDto(string Label, DateOnly? Start, DateOnly? EndExclusive,
    string Demand, string Receipts, string ProjectedQoh, string PlannedDue, string PlannedRelease, bool HasUnconfirmed);
public sealed record ComponentMrpScreenDto(string Site, string Part, DateOnly AsOfDate, int HorizonWeeks, string Period,
    bool IncludeUnconfirmed, DateOnly ForecastEndExclusive, DateTimeOffset AcquiredAtUtc, string AcquisitionId,
    DateTimeOffset ExpiresAtUtc, string? UnitOfMeasure, string AdjustedOpeningQoh, string? SafetyStock,
    string? DataQualityWarning, IReadOnlyList<ComponentMrpPeriodDto> Periods);
public sealed record ComponentMrpPreparedDto(string PreparationId, string FileName, DateOnly AsOfDate,
    DateTimeOffset AcquiredAtUtc, DateTimeOffset ExpiresAtUtc, string? Warning, string Period, bool IncludeUnconfirmed, DateOnly ForecastEndExclusive);

namespace Kst.Api.Dtos;

public sealed record PartPriceBreakDto(
    decimal MinimumOrderQuantity,
    decimal UnitPrice
);

public sealed record PartDetailResponseDto(
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
    IReadOnlyList<PartPriceBreakDto> PriceBreaks,
    DateTimeOffset LoadedAtUtc,
    bool IsStale,
    string? Warning
);

namespace Kst.Api.Dtos;

public sealed record ComponentOrderLineDto(
    string ComponentPart,
    string? Description,
    int? LeadTimeDays,
    string PoNumber,
    int PoLine,
    DateOnly? DueDate,
    decimal OpenQuantity,
    bool? Confirmed,
    string? SupplierDisplay,
    string? BuyerDisplay,
    string? ManufacturerItem,
    bool IsKss,
    string? TrackingInfo,
    bool? IsCreditHold,
    bool? IsCia,
    string? CurrentComments
);

public sealed record ComponentOrderGroupDto(
    string ComponentPart,
    ComponentOrderLineDto DisplayLine,
    IReadOnlyList<ComponentOrderLineDto> AdditionalLines
);

public sealed record ComponentOrdersResponseDto(
    string SnapshotId,
    string EnrichmentAvailability,
    IReadOnlyList<ComponentOrderGroupDto> Groups
);

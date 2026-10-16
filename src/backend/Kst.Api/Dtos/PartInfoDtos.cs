namespace Kst.Api.Dtos;

public sealed record PartInfoSpecDto(IReadOnlyList<string> Sites, int MaximumPartLength);
public sealed record PartInfoLocationsDto(string Site, string Part, PartInfoSectionDto<IReadOnlyList<InventoryLocationRowDto>> Locations);
public sealed record PartInfoSectionDto<T>(T? Value, DateTimeOffset? AcquiredAtUtc, string? Error);
public sealed record PartInfoMasterDto(string? Description, string? PartStatusCode, string? PartStatusDescription,
    string? IosCode, string? StandardCost, string? Qctc, int? TimeFence, string? SafetyTime, string? SafetyStock,
    string? BuyerPlanner, int? PurchaseLeadTimeDays, int? InspectionLeadTimeDays, int? CumulativeLeadTimeDays,
    string? MinimumOrderQuantity, string? OrderMultiple);
public sealed record PartInfoInventoryDto(string NetQuantityOnHand, string NonNetQuantityOnHand);
public sealed record PartInfoContextDto(string? ManufacturerItem, string? EffectivePmCode, int? ManufacturingLeadWorkingDays);
public sealed record PartInfoSupplierDto(string Identifier, string? Display, bool? IsCreditHold, bool? IsCia);
public sealed record PartInfoOrderDto(string PoNumber, int PoLine, DateOnly? DueDate, string OpenQuantity,
    bool? Confirmed, string? SupplierIdentifier, string? SupplierDisplay, string? BuyerDisplay, string? ManufacturerItem, string? TrackingInfo);
public sealed record PartInfoResponseDto(string Site, string Part, PartInfoMasterDto Master,
    PartInfoSectionDto<PartInfoInventoryDto> Inventory,
    PartInfoSectionDto<IReadOnlyList<InventoryLocationRowDto>> Locations,
    PartInfoSectionDto<IReadOnlyList<ApprovedVendorDto>> Alternates,
    PartInfoSectionDto<PartInfoContextDto> Context,
    PartInfoSectionDto<IReadOnlyList<PartInfoOrderDto>> Orders,
    PartInfoSectionDto<string> Comment,
    PartInfoSectionDto<IReadOnlyList<PartInfoSupplierDto>> Suppliers);

namespace Kst.Domain.Shortages;

/// <summary>Normalized Stage 9 requirement for one WO/component, after the source-specific EA rule is applied.</summary>
public sealed record ComponentRequirement(
    string ComponentPart,
    string? Description,
    RequirementSource Source,
    decimal AdjustedRequiredQuantity,
    decimal IssuedQuantity,
    string? UnitOfMeasure,
    bool IsReliable = true,
    bool IsManufactured = false);

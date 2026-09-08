namespace Kst.Domain.Shortages;

/// <summary>
/// Site/component inventory position. Activity quantities are explanatory context and never increase
/// <see cref="UsableQuantity"/>.
/// </summary>
public sealed record UsableInventoryPosition(
    decimal UsableQuantity,
    IReadOnlyDictionary<InventoryActivity, decimal> ActivityQuantities,
    bool IsReliable = true);

namespace Kst.Domain.Inventory;

/// <summary>One physical inventory row. Text and exact source quantity are preserved, including nulls.</summary>
public sealed record InventoryLocationRow(
    string RowId, string? Location, string? LotSerial, string? Status, string? QuantityOnHand);

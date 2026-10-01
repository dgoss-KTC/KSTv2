namespace Kst.Domain.OpenOrders;

/// <summary>Minimal fresh source facts required to authorize an operational file.</summary>
public sealed record OpenOrderCurrentLine(OpenOrderLineKey Key, string Site, string ItemNumber,
    decimal ShippedQty, OpenOrderEditableValues Values);

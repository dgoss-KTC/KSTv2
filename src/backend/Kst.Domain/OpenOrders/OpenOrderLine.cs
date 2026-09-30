namespace Kst.Domain.OpenOrders;

/// <summary>Source identity is domain + sales order + detail line; site and item retain scope provenance.</summary>
public sealed record OpenOrderLineKey(string Domain, string SalesOrder, int Line);

public sealed record OpenOrderEditableValues(
    DateOnly? DueDate, DateOnly? PerformDate, DateOnly? RequiredDate, DateOnly? DockDate,
    decimal OrderQty, decimal Price);

/// <summary>Full report facts, including non-visible filter and planning inputs.</summary>
public sealed record OpenOrderLine(
    OpenOrderLineKey Key, string ItemNumber, string Site, string? PurchaseOrder,
    string? Stat, decimal ShippedQty, OpenOrderEditableValues SourceValues,
    decimal? Allocated, string? Customer, string? CustomerName, string? Salesperson,
    string? CustomerPart, string? Ios, string LineComments, string? LineHold,
    bool? Partials, decimal? Picked, string? Plnr, string? ProdStat,
    string? ProductLine, string? QaHold, string? Remarks, string? Revision,
    string? ShipAcct, string? ShipTo, string? ShipVia, decimal? SiteQoh,
    string? SoHoldStatus, string? SoType, bool? Consignment)
{
    public decimal Open => SourceValues.OrderQty - ShippedQty;
    // Legacy display-only consignment adjustment must never change the raw price or extension.
    public decimal UnitPrice => Consignment == true ? 0m : SourceValues.Price;
    public decimal ExtPrice => SourceValues.Price * Open;
}

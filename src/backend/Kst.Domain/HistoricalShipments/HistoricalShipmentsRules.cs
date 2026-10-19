namespace Kst.Domain.HistoricalShipments;

public sealed record HistoricalShipmentsFilter(string Site, string? CorpGroup,
    string? CustomerFrom, string? CustomerTo, string? ItemFrom, string? ItemTo,
    string? ProductLineFrom, string? ProductLineTo, string? SalespersonFrom, string? SalespersonTo,
    DateOnly? InvoiceDateFrom, DateOnly? InvoiceDateTo, IReadOnlyList<string> Columns);
public sealed record ShipmentColumn(string Id, string Label, string Kind, string Provenance,
    bool DefaultSelected = false, bool Monetary = false);
public sealed record ShipmentRow(string PhysicalDetailId, IReadOnlyList<string?> Cells, bool UnavailableEnrichment = false);
public sealed class ShipmentSourceException(string message) : Exception(message);
public sealed class ShipmentWorkbookCapacityException(string message) : Exception(message);

public static class HistoricalShipmentsRules
{
    public const int MaximumPageSize = 1000;
    public const int MaximumXlsxDataRows = 1048575;
    public static readonly IReadOnlyList<string> Sites = Array.AsReadOnly(new[] { "SW", "NW", "AR", "MN", "MS", "KV" });
    public static readonly IReadOnlyList<ShipmentColumn> Columns = Array.AsReadOnly(new ShipmentColumn[]
    {
        new("site", "Site", "text", "Recorded invoice detail"),
        new("customerNumber", "Customer Number", "text", "Recorded invoice header"),
        new("customerName", "Customer Name (current master)", "text", "Current customer master"),
        new("invoiceNumber", "Invoice Number", "text", "Recorded invoice header", true),
        new("invoiceDate", "Invoice Date", "date", "Recorded invoice header"),
        new("itemNumber", "Part", "text", "Recorded invoice detail idh_part", true),
        new("invoicedQuantity", "Qty Shipped", "decimal", "Recorded invoiced quantity idh_qty_inv; signed, no normalization; does not establish net physical shipments", true),
        new("salesOrder", "Sales Order", "text", "Recorded invoice header ih_nbr; distinct from detail idh_nbr", true),
        new("salesOrderLine", "Line", "text", "Recorded invoice detail idh_line", true),
        new("customerPo", "Customer PO", "text", "Recorded invoice header", true),
        new("shipTo", "Ship To", "text", "Recorded invoice header ih_ship identifier", true),
        new("shipToName", "Ship-to Name (current address)", "text", "Current address master; no customer fallback"),
        new("customerItem", "Customer Item Number", "text", "Recorded invoice detail"),
        new("description1", "Item Description 1 (current master)", "text", "Current part master"),
        new("description2", "Item Description 2 (current master)", "text", "Current part master"),
        new("shipDate", "Ship Date", "date", "Recorded invoice header ih_ship_date", true),
        new("orderDate", "Order Date", "date", "Recorded invoice header"),
        new("dueDate", "Due Date", "date", "Recorded invoice detail idh_due_date"),
        new("originalCommit", "Perform Date", "date", "Recorded invoice detail idh_per_date", true),
        new("unitPrice", "Original Invoice Unit Price", "decimal", "Recorded idh_price in original invoice currency; no retro-price substitution", false, true),
        new("extendedPrice", "Original Invoice Extended Value", "decimal", "Exact idh_qty_inv × idh_price in original currency; not accounting revenue", false, true),
        new("currency", "Currency", "text", "Recorded invoice header"),
        new("exchangeRate", "Recorded Exchange Rate", "decimal", "Recorded invoice header; informational, not applied"),
        new("lineType", "Invoice Detail Type", "text", "Recorded invoice detail"),
        new("shipVia", "Ship Via", "text", "Recorded invoice header ih_shipvia", true),
        new("billOfLading", "BOL", "text", "Recorded invoice header ih_bol", true),
        new("boxes", "Boxes (invoice header)", "decimal", "Recorded ih__qad03"),
        new("pallets", "Pallets (invoice header)", "decimal", "Recorded ih__dec01"),
        new("salesperson", "Salesperson (position 1)", "text", "Recorded invoice header position 1"),
        new("shippingAccount", "Shipping Account", "text", "Recorded ih__chr01"),
        new("ncAccount", "NC Account", "text", "Recorded ih__chr05; legacy label"),
        new("release", "Release Number", "text", "Recorded idh__chr09"),
        new("remarks", "Invoice Remarks", "text", "Recorded invoice header"),
        new("productLine", "Product Line", "text", "Recorded invoice detail"),
        new("channel", "SO Type", "text", "Recorded invoice header ih_channel", true),
        new("recordedUom", "Recorded Invoice UOM", "text", "Literal idh_um; no normalization or inferred quantity semantics"),
        new("requiredDate", "Required Date", "date", "Recorded invoice detail idh_req_date"),
        new("retroUnitPrice", "Unit Price", "decimal", "Recorded idh_retro_price in original invoice currency; null remains unavailable, no idh_price fallback", true, true),
        new("retroExtendedPrice", "Ext Price", "decimal", "Exact signed idh_qty_inv × idh_retro_price in original currency; not accounting revenue", true, true),
        new("onTime", "On Time", "decimal", "Standard ih_channel: idh_due_date <= idh_per_date yields 1, > yields 0; otherwise/null dates yield null. Underlying datetime comparison, not actual-ship punctuality", true),
        new("detailSalesOrder", "Sales Order (invoice detail)", "text", "Recorded invoice detail idh_nbr; distinct from header ih_nbr")
    });

    // Public catalog order is independent of the fixed acquisition schema; shared by Reports and future consumers.
    public static readonly IReadOnlyList<string> DefaultColumnIds = Array.AsReadOnly(new[] {
        "shipDate", "originalCommit", "salesOrder", "customerPo", "salesOrderLine",
        "itemNumber", "invoicedQuantity", "retroUnitPrice", "retroExtendedPrice", "shipTo", "shipVia",
        "billOfLading", "invoiceNumber", "channel", "onTime" });
    public static readonly IReadOnlyList<ShipmentColumn> Catalog = Array.AsReadOnly(
        DefaultColumnIds.Select(id => Columns.Single(c => c.Id == id)).Concat(Columns.Where(c => !c.DefaultSelected)).ToArray());

    public static HistoricalShipmentsFilter Validate(HistoricalShipmentsFilter f)
    {
        string? Bound(string? s, int max)
        {
            s = string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            if (s?.Length > max || s?.Any(char.IsControl) == true) throw new ArgumentException("Invalid filter length or control character.");
            return s;
        }
        var site = f.Site?.Trim().ToUpperInvariant();
        if (site is null || !Sites.Contains(site)) throw new ArgumentException("Select one supported site.");
        f = f with
        {
            Site = site,
            CorpGroup = Bound(f.CorpGroup, 20),
            CustomerFrom = Bound(f.CustomerFrom, 80),
            CustomerTo = Bound(f.CustomerTo, 80),
            ItemFrom = Bound(f.ItemFrom, 30),
            ItemTo = Bound(f.ItemTo, 30),
            ProductLineFrom = Bound(f.ProductLineFrom, 30),
            ProductLineTo = Bound(f.ProductLineTo, 30),
            SalespersonFrom = Bound(f.SalespersonFrom, 82),
            SalespersonTo = Bound(f.SalespersonTo, 82)
        };
        if (f.CorpGroup is not null && (f.CustomerFrom is not null || f.CustomerTo is not null))
            throw new ArgumentException("Corp Group and Customer range are mutually exclusive.");
        foreach (var (from, to) in new[] { (f.CustomerFrom, f.CustomerTo), (f.ItemFrom, f.ItemTo),
                     (f.ProductLineFrom, f.ProductLineTo), (f.SalespersonFrom, f.SalespersonTo) })
            if (from is not null && to is not null && string.CompareOrdinal(from, to) > 0)
                throw new ArgumentException("Range start must not exceed range end.");
        if (f.InvoiceDateFrom > f.InvoiceDateTo) throw new ArgumentException("Invoice date start must not exceed end.");
        if (f.Columns is null || f.Columns.Count == 0 || f.Columns.Count > Columns.Count ||
            f.Columns.Distinct(StringComparer.Ordinal).Count() != f.Columns.Count ||
            f.Columns.Any(id => !Columns.Any(c => c.Id == id))) throw new ArgumentException("Select valid, distinct report columns.");
        return f with { Columns = f.Columns.ToList().AsReadOnly() };
    }
}

using Kst.Domain.OpenOrders;

namespace Kst.Exports;

/// <summary>Allowlisted report-only columns. Never accept a client-supplied header or value.</summary>
public static class OpenOrdersReportColumns
{
    public static readonly IReadOnlyDictionary<string, (string Label, Func<OpenOrderLine, object?> Value)> All =
        new Dictionary<string, (string, Func<OpenOrderLine, object?>)>
        {
            ["dueDate"] = ("Due Date", r => r.SourceValues.DueDate),
            ["order"] = ("SO", r => r.Key.SalesOrder),
            ["po"] = ("PO", r => r.PurchaseOrder),
            ["line"] = ("Line", r => r.Key.Line),
            ["itemNumber"] = ("Item Number", r => r.ItemNumber),
            ["site"] = ("Site", r => r.Site),
            ["open"] = ("Open", r => r.Open),
            ["stat"] = ("Status", r => r.Stat),
            ["extPrice"] = ("Ext Price", r => r.ExtPrice),
            ["allocated"] = ("Allocated", r => r.Allocated),
            ["customer"] = ("Customer #", r => r.Customer),
            ["customerName"] = ("Customer Name", r => r.CustomerName),
            ["customerPart"] = ("Customer Part", r => r.CustomerPart),
            ["dockDate"] = ("Dock Date", r => r.SourceValues.DockDate),
            ["ios"] = ("IOS", r => r.Ios),
            ["lineComments"] = ("Line Comments", r => r.LineComments),
            ["lineHold"] = ("Line Hold", r => r.LineHold),
            ["partials"] = ("Partials", r => r.Partials),
            ["performDate"] = ("Perform Date", r => r.SourceValues.PerformDate),
            ["picked"] = ("Picked", r => r.Picked),
            ["plnr"] = ("Planner", r => r.Plnr),
            ["prodStat"] = ("Prod Stat", r => r.ProdStat),
            ["productLine"] = ("Product Line", r => r.ProductLine),
            ["qaHold"] = ("QA Hold", r => r.QaHold),
            ["remarks"] = ("Remarks", r => r.Remarks),
            ["requiredDate"] = ("Required Date", r => r.SourceValues.RequiredDate),
            ["revision"] = ("Revision", r => r.Revision),
            ["shipAcct"] = ("Ship Acct", r => r.ShipAcct),
            ["shipTo"] = ("Ship To", r => r.ShipTo),
            ["shipVia"] = ("Ship Via", r => r.ShipVia),
            ["siteQoh"] = ("Site QOH", r => r.SiteQoh),
            ["soHoldStatus"] = ("SO Hold Status", r => r.SoHoldStatus),
            ["soType"] = ("SO Type", r => r.SoType),
            ["unitPrice"] = ("Unit Price", r => r.UnitPrice),
        };
}

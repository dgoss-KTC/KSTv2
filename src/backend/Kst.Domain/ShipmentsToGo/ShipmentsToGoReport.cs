using System.Globalization;
using System.Text.RegularExpressions;

namespace Kst.Domain.ShipmentsToGo;

/// <summary>Query observations, not unique-key records. Preserve repeated joined observations.</summary>
public sealed record ShipmentToGoLine(DateTime DueDate, string Order, string? PurchaseOrder, string? Channel,
    int Line, string? Ios, string ItemNumber, string? ProductLine, string Site, string? OrderSite,
    double Open, double? Allocated, double? Picked, double Qoh, string? ShipToName,
    double? ExtendedPrice, string? Crc, double? Risk, string? Comments)
{
    public string Soln => Order + Line.ToString(CultureInfo.InvariantCulture);
}

public sealed record ShipmentToGoSheet(string Name, string TableName, IReadOnlyList<ShipmentToGoLine> Lines);
public sealed record ShipmentsToGoReport(IReadOnlyList<ShipmentToGoSheet> Sheets,
    IReadOnlyDictionary<string, double> ShippedToday, DateOnly LabelDate, DateTimeOffset AcquiredAtUtc);

public static class ShipmentsToGoRules
{
    public static bool ValidSites(IReadOnlyList<string>? sites) => sites is { Count: > 0 and <= 6 }
        && sites.All(s => s is "SW" or "NW" or "AR" or "MN" or "MS" or "KV")
        && sites.Distinct(StringComparer.Ordinal).Count() == sites.Count;

    /// <summary>Fixture clock equivalent of the English SQL DATENAME CASE; production uses GETDATE.</summary>
    public static DateOnly Cutoff(DateOnly serverDate, bool includeNextWeek) =>
        serverDate.AddDays((serverDate.DayOfWeek == DayOfWeek.Saturday ? 7 : 6 - (int)serverDate.DayOfWeek)
            + (includeNextWeek ? 7 : 0));

    public static IReadOnlyList<ShipmentToGoLine> Process(IReadOnlyList<ShipmentToGoLine> source)
    {
        // Donor order is the acquired SQL order, before repairs and No Stock. No scope/donor corrections.
        var donors = source.Where(r => r.Comments is not null && r.Crc is not null).ToArray();
        var rows = source.Select(row =>
        {
            var crc = row.Crc;
            if (string.IsNullOrWhiteSpace(crc) || string.Equals(crc?.ToUpperInvariant(), "DEBBI EVANS", StringComparison.Ordinal))
            {
                var pattern = new Regex($"(^|[^A-Za-z0-9]){Regex.Escape(row.Order)}([^A-Za-z0-9]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                var donor = donors.FirstOrDefault(d => pattern.IsMatch(d.Comments!));
                crc = (donor?.Crc ?? crc)?.Trim();
            }
            var comments = row.Qoh <= 0 ? "No Stock; " + (row.Comments ?? "") : row.Comments;
            var risk = row.Risk;
            if (comments?.Contains("Memo", StringComparison.Ordinal) == true && row.Channel == "Standard") risk = 0;
            if (comments?.Contains("ADJ", StringComparison.Ordinal) == true) risk = -row.ExtendedPrice;
            return row with { Crc = crc, Comments = comments, Risk = risk,
                Site = row.Site.Trim().ToUpperInvariant(), OrderSite = row.OrderSite?.Trim().ToUpperInvariant() };
        });
        return rows.OrderBy(r => r.Crc, StringComparer.Ordinal).ThenBy(r => r.DueDate)
            .ThenBy(r => r.Order, StringComparer.Ordinal).ThenBy(r => r.Line).ToArray();
    }

    public static ShipmentsToGoReport Build(IReadOnlyList<string> sites, IReadOnlyList<ShipmentToGoLine> source,
        IReadOnlyDictionary<string, double> shipped, DateOnly labelDate, DateTimeOffset acquired)
    {
        if (!ValidSites(sites)) throw new ArgumentException("Select supported sites.");
        var rows = Process(source);
        var mainSites = sites.Where(s => s is not ("AR" or "KV")).ToArray();
        var mainName = mainSites.Length == 1 ? $"KTC-{mainSites[0]} S2G" : "KTC-SW S2G";
        var sheets = new List<ShipmentToGoSheet>();
        void Add(string name, string table, IEnumerable<ShipmentToGoLine> lines)
        {
            var included = lines.ToArray();
            if (included.Length != 0) sheets.Add(new(name, table, included));
        }
        Add(mainName, "stg_main_post", rows.Where(r => r.OrderSite != "AR" && r.Site != "KV"));
        Add("KTC-AR S2G", "stg_ar_post", rows.Where(r => r.OrderSite == "AR"));
        Add("KTV S2G", "stg_ktv_post", rows.Where(r => r.OrderSite != "AR" && r.Site == "KV"));
        return new(sheets, shipped, labelDate, acquired);
    }
}

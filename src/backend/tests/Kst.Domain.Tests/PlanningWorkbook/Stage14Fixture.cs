using System.Globalization;
using System.Text.Json;
using Kst.Domain.Mps;
using Kst.Domain.OpenOrders;
using Kst.Domain.PartDetail;
using Kst.Domain.PlanningWorkbook;

namespace Kst.Domain.Tests.PlanningWorkbook;

/// <summary>Loads the accepted Stage 14 synthetic fixture into Domain inputs. Never production data.</summary>
internal sealed class Stage14Fixture
{
    public const string FixtureRelativePath = "docs/reference/STAGE_14_PLANNING_WORKBOOK_DETERMINISTIC_FIXTURES.json";

    private Stage14Fixture(JsonElement root, string repositoryRoot)
    {
        Root = root;
        RepositoryRoot = repositoryRoot;
        var scope = root.GetProperty("scope");
        LoadedSnapshotId = scope.GetProperty("mpsSnapshotId").GetGuid();
        ResolvedParts = scope.GetProperty("resolvedParts").EnumerateArray()
            .Select(p => new MpsResolvedPart(p.GetString()!, null)).ToList();
        var calendar = root.GetProperty("calendar");
        Today = Date(calendar.GetProperty("today"));
        HorizonWeeks = calendar.GetProperty("horizonWeeks").GetInt32();
        WeekLabels = calendar.GetProperty("weekLabels").EnumerateArray().Select(Date).ToList();

        var inputs = root.GetProperty("inputs");
        MpsRows = inputs.GetProperty("mpsSourceRows").EnumerateArray().Select(r => new MpsSourceRow(
            scope.GetProperty("domain").GetString()!, scope.GetProperty("site").GetString()!,
            r.GetProperty("part").GetString()!, null, Date(r.GetProperty("dueDate")), null,
            Dec(r, "quantity"), SupplyType(r.GetProperty("mrpType").GetString()!),
            $"WO-{r.GetProperty("part").GetString()}-{r.GetProperty("dueDate").GetString()}",
            State(r.GetProperty("workOrderState").GetString()!))).ToList();
        SalesOrderLines = inputs.GetProperty("openOrderLines").EnumerateArray().Select(SalesLine).ToList();
        NetQoh = inputs.GetProperty("netQoh").EnumerateObject()
            .ToDictionary(p => p.Name, p => Parse(p.Value.GetString()!));
        PriceTiers = inputs.GetProperty("priceTiers").EnumerateObject().ToDictionary(
            p => p.Name,
            p => (IReadOnlyList<PartPriceBreak>)p.Value.EnumerateArray()
                .Select(t => new PartPriceBreak(Dec(t, "minQty"), Dec(t, "price"))).ToList());
        ForecastRows = inputs.GetProperty("forecastRows").EnumerateArray().Select(f => new PlanningForecastRow(
            f.GetProperty("part").GetString()!, Date(f.GetProperty("dueDate")), Dec(f, "mrpQty"))).ToList();
        ScenarioAdjustments = root.GetProperty("adjustmentScenario").GetProperty("adjustments").EnumerateArray()
            .Select(a => new ProposedMpsAdjustment(a.GetProperty("part").GetString()!, Date(a.GetProperty("weekLabel")), Dec(a, "adjustment")))
            .ToList();
    }

    public JsonElement Root { get; }
    public string RepositoryRoot { get; }
    public Guid LoadedSnapshotId { get; }
    public IReadOnlyList<MpsResolvedPart> ResolvedParts { get; }
    public DateOnly Today { get; }
    public int HorizonWeeks { get; }
    public IReadOnlyList<DateOnly> WeekLabels { get; }
    public IReadOnlyList<MpsSourceRow> MpsRows { get; }
    public IReadOnlyList<OpenOrderLine> SalesOrderLines { get; }
    public IReadOnlyDictionary<string, decimal> NetQoh { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<PartPriceBreak>> PriceTiers { get; }
    public IReadOnlyList<PlanningForecastRow> ForecastRows { get; }
    public IReadOnlyList<ProposedMpsAdjustment> ScenarioAdjustments { get; }

    public PlanningWorkbookInput Input(
        IReadOnlyList<ProposedMpsAdjustment>? adjustments = null,
        IReadOnlyList<PlanningForecastRow>? forecast = null,
        IReadOnlyList<OpenOrderLine>? salesOrders = null,
        IReadOnlyList<MpsSourceRow>? mpsRows = null) =>
        new(ResolvedParts, mpsRows ?? MpsRows, salesOrders ?? SalesOrderLines, NetQoh, PriceTiers,
            forecast ?? ForecastRows, adjustments ?? ScenarioAdjustments, HorizonWeeks, Today);

    public MpsAdjustmentContext Context(IReadOnlyList<MpsSourceRow>? mpsRows = null) =>
        new(LoadedSnapshotId, ResolvedParts, mpsRows ?? MpsRows, Today);

    public static Stage14Fixture Load()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, FixtureRelativePath);
            if (File.Exists(path))
            {
                using var document = JsonDocument.Parse(File.ReadAllBytes(path));
                return new Stage14Fixture(document.RootElement.Clone(), dir.FullName);
            }
        }
        throw new DirectoryNotFoundException("Stage 14 reference fixture not found.");
    }

    public static decimal Parse(string text) =>
        decimal.Parse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    public static decimal Dec(JsonElement element, string property) => Parse(element.GetProperty(property).GetString()!);

    public static decimal? NullableDec(JsonElement element, string property) =>
        element.GetProperty(property).ValueKind == JsonValueKind.Null ? null : Dec(element, property);

    public static DateOnly Date(JsonElement element) =>
        DateOnly.ParseExact(element.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Builds an accepted Stage 13 line whose Open and ExtPrice equal the fixture facts.</summary>
    private static OpenOrderLine SalesLine(JsonElement row)
    {
        var open = Dec(row, "open");
        var ext = Dec(row, "extPrice");
        var due = row.GetProperty("dueDate").ValueKind == JsonValueKind.Null ? (DateOnly?)null : Date(row.GetProperty("dueDate"));
        var values = new OpenOrderEditableValues(due, null, null, null, open, ext / open);
        var line = new OpenOrderLine(
            new OpenOrderLineKey("TEST", row.GetProperty("salesOrder").GetString()!, row.GetProperty("line").GetInt32()),
            row.GetProperty("itemNumber").GetString()!, "S1", null, null, 0m, values,
            null, null, null, null, null, null, "", null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null);
        if (line.Open != open || line.ExtPrice != ext)
            throw new InvalidOperationException("Fixture sales line cannot be represented exactly as Stage 13 facts.");
        return line;
    }

    private static MpsSupplyType SupplyType(string value) => value switch
    {
        "SUPPLY" => MpsSupplyType.Supply,
        "SUPPLYF" => MpsSupplyType.SupplyF,
        "SUPPLYP" => MpsSupplyType.SupplyP,
        _ => throw new InvalidOperationException(value)
    };

    private static MpsWorkOrderState State(string value) => value switch
    {
        "A" => MpsWorkOrderState.Allocating,
        "F" => MpsWorkOrderState.Frozen,
        "R" => MpsWorkOrderState.Released,
        "P" => MpsWorkOrderState.Planned,
        "E" => MpsWorkOrderState.ExplicitlyScheduled,
        _ => MpsWorkOrderState.Unknown
    };
}

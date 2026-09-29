using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kst.Api.Dtos;
using Kst.Api.Endpoints;
using Kst.Domain.LongTermShortages;

// Screen-only transport prototype: one metadata row, two compact mode vectors. Full evidence
// remains in the original immutable projection. No application endpoint/contract is changed.
sealed record ScreenMode(string Severity, DateOnly? FirstShortDate, DateOnly? FirstAtRiskDate,
    decimal MaximumShortage, DateOnly? FirstRecoveryDate, IReadOnlyList<decimal> Ending, IReadOnlyList<string> WeeklySeverity);
sealed record ScreenComponent(string ComponentPart, string? UnitOfMeasure, string? QadStatus, string? Description,
    string? Planner, string? BuyerPlannerCode, decimal OpeningQoh, decimal? SafetyStock, string? DataQualityWarning,
    bool IsKss, ScreenMode Confirmed, ScreenMode All);
sealed record ScreenResponse(string SnapshotId, DateOnly RefreshDate, bool IsStale, string? Warning,
    DateTimeOffset AcquiredAtUtc, string ConsistencyMode, IReadOnlyList<DateOnly?> WeekStarts, IReadOnlyList<ScreenComponent> Components);

static class ArchitecturePrototypes
{
    public static void Run(string mode, string directory, JsonSerializerOptions json)
    {
        var destination = Path.Combine(directory, $"architecture-{mode}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.jsonl");
        void Print(object value)
        {
            var line = JsonSerializer.Serialize(value, json);
            File.AppendAllText(destination, line + Environment.NewLine);
            Console.WriteLine(line);
        }
        if (mode == "split-prototype") { Split(directory, json, Print); return; }
        if (mode == "algorithm-regressions") { Regressions(json, Print); return; }
        // Resume only workbook checks already persisted by this harness. Projection equivalence
        // is still rechecked below. This avoids repeating expensive XLSX work after a deadline.
        var completedWorkbooks = new HashSet<string>();
        if (mode == "algorithm-workbooks")
            foreach (var previous in Directory.GetFiles(directory, "architecture-algorithm-workbooks-*.jsonl"))
            foreach (var line in File.ReadLines(previous))
            {
                using var record = JsonDocument.Parse(line);
                var r = record.RootElement;
                if (r.GetProperty("phase").GetString() == "workbook" && r.GetProperty("equivalent").GetBoolean())
                    completedWorkbooks.Add($"{r.GetProperty("workspace").GetString()}/{r.GetProperty("variant").GetString()}/{r.GetProperty("includeUnconfirmed").GetBoolean()}");
            }
        foreach (var file in Directory.GetFiles(directory, "*-inputs.json").Order())
        {
            var capture = JsonSerializer.Deserialize<Capture>(File.ReadAllText(file), json)!;
            var name = Path.GetFileName(file).Replace("-inputs.json", "");
            var baseline = File.ReadAllBytes(Path.Combine(directory, name + "-projection.json"));
            var confirmed = IndexedProjectionReference.Build(capture.AsOf, capture.Acquisition.Inputs);
            var all = IndexedProjectionReference.Build(capture.AsOf, capture.Acquisition.Inputs, includeUnconfirmed: true);
            if (!baseline.AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(new { confirmed, all }, json)))
                throw new InvalidOperationException("Current production differs from retained capture.");
            if (mode == "contract-prototype")
            {
                var map = typeof(LongTermShortagesEndpoints).GetMethod("MapForResponse", BindingFlags.NonPublic | BindingFlags.Static)!
                    .CreateDelegate<Func<LongTermShortageRow, bool, LongTermShortageRowDto>>();
                for (var run = 0; run < 7; run++)
                foreach (var compact in run % 2 == 0 ? new[] { false, true } : new[] { true, false })
                {
                    var allocated = GC.GetTotalAllocatedBytes(true);
                    var watch = Stopwatch.StartNew();
                    object dto = compact ? Map(capture, confirmed, all) : new LongTermShortagesResponseDto(
                        "00000000-0000-0000-0000-000000000001", capture.AsOf, false, null,
                        confirmed.Select(r => map(r, false)).ToArray(), capture.Acquisition.AcquiredAtUtc,
                        LongTermShortageAcquisition.ConsistencyMode, all.Select(r => map(r, false)).ToArray()) { EvidenceIncluded = false };
                    var mappingMs = watch.Elapsed.TotalMilliseconds;
                    var allocation = GC.GetTotalAllocatedBytes(true) - allocated;
                    watch.Restart();
                    var bytes = JsonSerializer.SerializeToUtf8Bytes(dto, json);
                    var serializationMs = watch.Elapsed.TotalMilliseconds;
                    if (run == 0) File.WriteAllBytes(Path.Combine(directory, name + (compact ? "-compact" : "-control") + "-api.json"), bytes);
                    Print(new { phase = mode, workspace = name, run, compact, mappingMs, serializationMs, bytes = bytes.Length, mappingAllocatedBytes = allocation });
                }
                continue;
            }
            foreach (var run in Enumerable.Range(0, mode == "algorithm-workbooks" ? 1 : 7))
            foreach (var variant in run % 2 == 0 ? new[] { "indexed", "dense", "sparse" } : new[] { "sparse", "dense", "indexed" })
            {
                var allocated = GC.GetTotalAllocatedBytes(true);
                var watch = Stopwatch.StartNew();
                var pair = variant == "indexed"
                    ? (Confirmed: IndexedProjectionReference.Build(capture.AsOf, capture.Acquisition.Inputs), All: IndexedProjectionReference.Build(capture.AsOf, capture.Acquisition.Inputs, includeUnconfirmed: true))
                    : ProjectionAlternatives.Build(capture.AsOf, capture.Acquisition.Inputs, variant == "sparse");
                var elapsedMs = watch.Elapsed.TotalMilliseconds;
                var bytesAllocated = GC.GetTotalAllocatedBytes(true) - allocated;
                var bytes = JsonSerializer.SerializeToUtf8Bytes(new { confirmed = pair.Confirmed, all = pair.All }, json);
                var equivalent = baseline.AsSpan().SequenceEqual(bytes);
                Print(new { phase = mode, workspace = name, run, variant, elapsedMs, bytesAllocated, equivalent });
                if (!equivalent) throw new InvalidOperationException("Complete projection mismatch: " + variant + " / " + name);
                if (mode == "algorithm-workbooks" && variant != "indexed")
                {
                    foreach (var include in new[] { false, true })
                    {
                        if (completedWorkbooks.Contains($"{name}/{variant}/{include}")) continue;
                        var before = WorkbookHash(capture, name, include ? all : confirmed);
                        var after = WorkbookHash(capture, name, include ? pair.All : pair.Confirmed);
                        if (before != after) throw new InvalidOperationException("Workbook mismatch.");
                        Print(new { phase = "workbook", workspace = name, variant, includeUnconfirmed = include, equivalent = true, hash = after });
                    }
                }
            }
        }
    }

    private static ScreenResponse Map(Capture capture, IReadOnlyList<LongTermShortageRow> confirmed, IReadOnlyList<LongTermShortageRow> all)
    {
        var byPart = all.ToDictionary(r => r.ComponentPart, StringComparer.OrdinalIgnoreCase);
        ScreenMode Mode(LongTermShortageRow row) => new(row.Severity.ToString(), row.FirstShortDate, row.FirstAtRiskDate,
            row.Episodes.Count == 0 ? 0 : row.Episodes.Max(e => e.MaximumShortage), row.Episodes.FirstOrDefault()?.FirstRecoveryDate,
            Array.AsReadOnly(row.Weeks.Select(w => w.ProjectedQoh).ToArray()), Array.AsReadOnly(row.Weeks.Select(w => w.Severity.ToString()).ToArray()));
        return new("00000000-0000-0000-0000-000000000001", capture.AsOf, false, null, capture.Acquisition.AcquiredAtUtc,
            LongTermShortageAcquisition.ConsistencyMode, Array.AsReadOnly(confirmed.FirstOrDefault()?.Weeks.Select(w => w.WeekStart).ToArray() ?? []),
            Array.AsReadOnly(confirmed.Select(r => new ScreenComponent(r.ComponentPart, r.UnitOfMeasure, r.QadStatus, r.Description,
                r.Planner, r.BuyerPlannerCode, r.OpeningQoh, r.SafetyStock, r.DataQualityWarning, r.Presentation?.IsKss == true,
                Mode(r), Mode(byPart[r.ComponentPart]))).ToArray()));
    }

    private static void Regressions(JsonSerializerOptions json, Action<object> print)
    {
        var asOf = new DateOnly(2026, 9, 24);
        LongTermMrpFact Fact(int id, string type, DateOnly due, decimal quantity) => new(id, type, due, null, quantity, MrpScheduleCategory.Unclassified)
            { SourceRowId = id.ToString(), SourceLine2 = id.ToString(), IsPoReceipt = type == "SUPPLY", PoConfirmed = type == "SUPPLY" ? true : null };
        LongTermShortageInput Input(string part, decimal opening, params LongTermMrpFact[] facts) => new(part, "EA", null, null, null, null, opening, SafetyStockState.Resolved, 0, [], facts);
        var fixtures = new List<LongTermShortageInput>
        {
            Input("74320-27", 75000, Fact(1, "DEMAND", new(2026,9,15), 66931), Fact(2, "DEMAND", new(2026,9,22), 71215),
                Fact(3, "DEMAND", asOf, 1252), Fact(4, "SUPPLY", new(2026,9,23), 270000),
                Fact(5, "DEMAND", new(2026,10,2), 59325), Fact(6, "SUPPLY", new(2026,10,2), 270000)),
            Input("355203-PUR", 214, Fact(1, "DEMAND", new(2026,9,22), 1420), Fact(2, "SUPPLY", new(2026,9,26), 8000))
        };
        var random = new Random(110929);
        for (var sample = 0; sample < 100; sample++)
        {
            var facts = Enumerable.Range(0, 70).Select(id =>
            {
                var type = new[] { "DEMAND", "SUPPLY", "SUPPLYP", "SUPPLYF" }[random.Next(4)];
                var fact = Fact(id, type, asOf.AddDays(random.Next(-20, 520)), random.Next(-10, 1000) / 1000m);
                return fact with { ReleaseDate = asOf.AddDays(random.Next(-20, 520)),
                    IsPoReceipt = type == "SUPPLY" && random.Next(2) == 0,
                    PoConfirmed = type == "SUPPLY" ? random.Next(2) == 0 : null };
            }).ToArray();
            fixtures.Add(Input("synthetic-" + sample, random.Next(-100, 100) / 100m, facts) with
                { UnitOfMeasure = sample % 2 == 0 ? "EA" : "KG", SafetyStock = sample % 3 == 0 ? null : 1.234m });
        }
        foreach (var weeks in new[] { 13, 26, 52, 72 })
        {
            var expected = JsonSerializer.SerializeToUtf8Bytes(new { confirmed = IndexedProjectionReference.Build(asOf, fixtures, weeks),
                all = IndexedProjectionReference.Build(asOf, fixtures, weeks, true) }, json);
            var production = LongTermShortagesBuilder.BuildBoth(asOf, fixtures, weeks);
            if (!expected.AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(new { confirmed = production.Confirmed, all = production.All }, json)))
                throw new InvalidOperationException("Production dense signed/fractional/locked oracle mismatch.");
            print(new { phase = "production-dense-regressions", weeks, fixtures = fixtures.Count, equivalent = true });
            foreach (var sparse in new[] { false, true })
            {
                var pair = ProjectionAlternatives.Build(asOf, fixtures, sparse, weeks);
                var actual = JsonSerializer.SerializeToUtf8Bytes(new { confirmed = pair.Confirmed, all = pair.All }, json);
                if (!expected.AsSpan().SequenceEqual(actual)) throw new InvalidOperationException("Synthetic or locked oracle mismatch.");
                print(new { phase = "algorithm-regressions", sparse, weeks, fixtures = fixtures.Count, equivalent = true,
                    lockedOracles = new[] { "74320-27", "355203-PUR" } });
            }
        }
    }

    internal static string WorkbookHash(Capture capture, string name, IReadOnlyList<LongTermShortageRow> rows)
    {
        var bytes = new Kst.Exports.PlaceholderExportService().CreateLongTermShortagesWorkbook(name, capture.AsOf, rows,
            capture.Acquisition.AcquiredAtUtc, LongTermShortageAcquisition.ConsistencyMode);
        using var stream = new MemoryStream(bytes);
        using var book = new ClosedXML.Excel.XLWorkbook(stream);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var sheet in book.Worksheets)
        foreach (var cell in sheet.CellsUsed())
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { sheet.Name, Address = cell.Address.ToString(),
                Type = cell.DataType.ToString(), Value = cell.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), Style = cell.Style.ToString() }) + "\n"));
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void Split(string directory, JsonSerializerOptions json, Action<object> print)
    {
        var file = Directory.GetFiles(directory, "batch-250-*-raw.json").Single();
        var rows = JsonNode.Parse(File.ReadAllBytes(file))!.AsArray();
        var eventFields = new HashSet<string>(["MrpType", "DueDate", "ReleaseDate", "Quantity", "SourceNumber", "SourceLine", "SourceLine2", "SourceRowId", "IsPoReceipt", "PoConfirmed"], StringComparer.OrdinalIgnoreCase);
        var metadata = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        var narrow = new JsonArray();
        foreach (var node in rows)
        {
            var row = node!.AsObject();
            var part = row["ComponentPart"]!.GetValue<string>();
            var component = new JsonObject(row.Where(p => !eventFields.Contains(p.Key)).Select(p => KeyValuePair.Create(p.Key, p.Value?.DeepClone())));
            if (metadata.TryGetValue(part, out var existing) && !JsonNode.DeepEquals(existing, component))
                throw new InvalidOperationException("Conflicting component metadata in capture; split cannot assert one value.");
            metadata.TryAdd(part, component);
            narrow.Add(new JsonObject(row.Where(p => eventFields.Contains(p.Key) || p.Key == "ComponentPart").Select(p => KeyValuePair.Create(p.Key, p.Value?.DeepClone()))));
        }
        var reconstructed = new JsonArray();
        foreach (var node in narrow)
        {
            var row = node!.AsObject();
            var component = metadata[row["ComponentPart"]!.GetValue<string>()];
            var merged = (JsonObject)component.DeepClone();
            foreach (var p in row.Where(p => p.Key != "ComponentPart")) merged.Add(p.Key, p.Value?.DeepClone());
            reconstructed.Add(merged);
        }
        if (!JsonNode.DeepEquals(rows, reconstructed)) throw new InvalidOperationException("Split reconstruction changed raw values.");
        print(new { phase = "split-shape", rawRows = rows.Count, metadataRows = metadata.Count, narrowRows = narrow.Count,
            originalJsonBytes = JsonSerializer.SerializeToUtf8Bytes(rows, json).Length,
            splitJsonBytes = JsonSerializer.SerializeToUtf8Bytes(new { metadata = metadata.Values, events = narrow }, json).Length,
            equivalent = true, liveSqlMeasured = false });
    }
}

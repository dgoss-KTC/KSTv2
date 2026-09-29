using System.Diagnostics;
using System.Text.Json;
using Kst.Api.Endpoints;
using Kst.Application.LongTermShortages;
using Kst.Domain.Common;
using Kst.Domain.LongTermShortages;

// Offline production gates. Original inputs, payloads and successful prototype hashes are read-only.
static class ProductionVerification
{
    public static void Run(string mode, string directory, JsonSerializerOptions json)
    {
        var destination = Path.Combine(directory, $"{mode}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.jsonl");
        void Print(object value)
        {
            var text = JsonSerializer.Serialize(value, json);
            File.AppendAllText(destination, text + Environment.NewLine);
            Console.WriteLine(text);
        }
        foreach (var file in Directory.GetFiles(directory, "*-inputs.json").Order())
        {
            var capture = JsonSerializer.Deserialize<Capture>(File.ReadAllText(file), json)!;
            var name = Path.GetFileName(file).Replace("-inputs.json", "");
            if (mode == "production-dense")
            {
                var baseline = File.ReadAllBytes(Path.Combine(directory, name + "-projection.json"));
                for (var run = 0; run < 7; run++)
                foreach (var dense in run % 2 == 0 ? new[] { false, true } : new[] { true, false })
                {
                    var allocated = GC.GetTotalAllocatedBytes(true);
                    var watch = Stopwatch.StartNew();
                    var pair = dense ? LongTermShortagesBuilder.BuildBoth(capture.AsOf, capture.Acquisition.Inputs)
                        : (Confirmed: IndexedProjectionReference.Build(capture.AsOf, capture.Acquisition.Inputs),
                           All: IndexedProjectionReference.Build(capture.AsOf, capture.Acquisition.Inputs, includeUnconfirmed: true));
                    var elapsedMs = watch.Elapsed.TotalMilliseconds;
                    var bytesAllocated = GC.GetTotalAllocatedBytes(true) - allocated;
                    if (!baseline.AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(new { confirmed = pair.Confirmed, all = pair.All }, json)))
                        throw new InvalidOperationException("Production dense/reference mismatch: " + name);
                    Print(new { phase = mode, workspace = name, run, dense, elapsedMs, bytesAllocated, equivalent = true });
                }
                continue;
            }
            var confirmed = LongTermShortagesBuilder.Build(capture.AsOf, capture.Acquisition.Inputs);
            var all = LongTermShortagesBuilder.Build(capture.AsOf, capture.Acquisition.Inputs, includeUnconfirmed: true);
            if (!File.ReadAllBytes(Path.Combine(directory, name + "-projection.json")).AsSpan()
                .SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(new { confirmed, all }, json)))
                throw new InvalidOperationException("Complete projection mismatch: " + name);
            if (mode.EndsWith("workbooks"))
            {
                foreach (var include in new[] { false, true })
                {
                    var expected = Directory.GetFiles(directory, "architecture-algorithm-workbooks-*.jsonl")
                        .SelectMany(File.ReadLines).Select(line => JsonSerializer.Deserialize<JsonElement>(line))
                        .First(r => r.GetProperty("phase").GetString() == "workbook" && r.GetProperty("workspace").GetString() == name
                            && r.GetProperty("includeUnconfirmed").GetBoolean() == include).GetProperty("hash").GetString();
                    var actual = ArchitecturePrototypes.WorkbookHash(capture, name, include ? all : confirmed);
                    if (actual != expected) throw new InvalidOperationException("Workbook hash mismatch: " + name);
                    Print(new { phase = mode, workspace = name, includeUnconfirmed = include, equivalent = true, hash = actual });
                }
                continue;
            }
            var result = new LongTermShortagesResult(LongTermShortagesOutcomeKind.Loaded,
                new SnapshotId(Guid.Parse("00000000-0000-0000-0000-000000000001")), capture.AsOf, confirmed,
                AcquiredAtUtc: capture.Acquisition.AcquiredAtUtc, ConsistencyMode: LongTermShortageAcquisition.ConsistencyMode,
                AllReceiptsRows: all);
            for (var run = 0; run < 7; run++)
            {
                var allocated = GC.GetTotalAllocatedBytes(true);
                var watch = Stopwatch.StartNew();
                var dto = LongTermShortagesEndpoints.MapScreen(result);
                var mappingMs = watch.Elapsed.TotalMilliseconds;
                var mappingAllocatedBytes = GC.GetTotalAllocatedBytes(true) - allocated;
                watch.Restart();
                var bytes = JsonSerializer.SerializeToUtf8Bytes(dto, json);
                var serializationMs = watch.Elapsed.TotalMilliseconds;
                if (run == 0) File.WriteAllBytes(Path.Combine(directory, name + "-production-api.json"), bytes);
                Print(new { phase = mode, workspace = name, run, mappingMs, serializationMs, mappingAllocatedBytes, bytes = bytes.Length, projectionEquivalent = true });
            }
        }
    }
}

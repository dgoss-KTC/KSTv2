using System.Diagnostics;
using System.Text.Json;
using Kst.Domain.Workspaces;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using System.Reflection;
using System.Security.Cryptography;
using Dapper;
using Kst.Domain.LongTermShortages;
using Kst.Integrations.Qad;
using Kst.Integrations.Qad.Bom;
using Kst.Integrations.Qad.LongTermShortages;
using Kst.Integrations.Qad.ComponentOrders;
using Microsoft.Data.SqlClient;
using Kst.Api.Dtos;
using Kst.Api.Endpoints;

// Explicit local diagnostic entry point; never loaded by the application.
// Do not print configuration, connection strings, SQL parameters, or source facts.
if (args.Length != 1 || args[0] is not ("production-dense" or "production-dense-workbooks" or "production-contract" or "production-contract-workbooks" or "discover" or "capture" or "capture-after" or "replay" or "sql" or "sql-after" or "api" or "api-screen" or "plans" or "metadata" or "batch" or "compare" or "workbook" or "compare-projection" or "normalize" or "split-prototype" or "contract-prototype" or "algorithm-prototypes" or "algorithm-workbooks" or "algorithm-regressions"))
    throw new ArgumentException("Usage: Stage11Performance discover|capture|replay|sql|api");
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var captureDirectory = Path.GetFullPath("scripts/stage11-performance/captures");
Directory.CreateDirectory(captureDirectory);
if (args[0].StartsWith("production-"))
{
    ProductionVerification.Run(args[0], captureDirectory, json);
    return;
}
if (args[0] is "split-prototype" or "contract-prototype" or "algorithm-prototypes" or "algorithm-workbooks" or "algorithm-regressions")
{
    ArchitecturePrototypes.Run(args[0], captureDirectory, json);
    return;
}
if (args[0] == "normalize")
{
    var capture = JsonSerializer.Deserialize<Capture>(File.ReadAllText(Path.Combine(captureDirectory, "2140-inputs.json")), json)!;
    var file = Directory.GetFiles(captureDirectory, "batch-250-*-raw.json").First();
    var rawType = typeof(QadLongTermShortageSourceReader).GetNestedType("RawRow", BindingFlags.NonPublic)!;
    var method = typeof(NormalizationReplay).GetMethod("Measure")!.MakeGenericMethod(rawType);
    method.Invoke(null, [File.ReadAllText(file), capture, json]);
    return;
}
if (args[0] == "compare-projection")
{
    // The first API diagnostic build predates the projection change. Load its domain assembly
    // in isolation and verify its output against the immutable capture before timing it.
    var context = new System.Runtime.Loader.AssemblyLoadContext("baseline", isCollectible: true);
    var assembly = context.LoadFromAssemblyPath(Path.GetFullPath("scripts/stage11-performance/bin/api/Kst.Domain.dll"));
    var build = assembly.GetType("Kst.Domain.LongTermShortages.LongTermShortagesBuilder")!.GetMethod("Build")!;
    foreach (var name in new[] { "2140", "2141", "2142" })
    {
        var capture = JsonSerializer.Deserialize<Capture>(File.ReadAllText(Path.Combine(captureDirectory, name + "-inputs.json")), json)!;
        var oldInputs = JsonSerializer.Deserialize(JsonSerializer.Serialize(capture.Acquisition.Inputs, json), build.GetParameters()[1].ParameterType, json);
        for (var run = 0; run < 7; run++)
        foreach (var before in run % 2 == 0 ? new[] { true, false } : new[] { false, true })
        {
            var allocated = GC.GetTotalAllocatedBytes(true);
            var timer = Stopwatch.StartNew();
            object confirmed = before ? build.Invoke(null, [capture.AsOf, oldInputs, 26, false])! : LongTermShortagesBuilder.Build(capture.AsOf, capture.Acquisition.Inputs);
            object all = before ? build.Invoke(null, [capture.AsOf, oldInputs, 26, true])! : LongTermShortagesBuilder.Build(capture.AsOf, capture.Acquisition.Inputs, includeUnconfirmed: true);
            var elapsed = timer.Elapsed.TotalMilliseconds;
            var bytesAllocated = GC.GetTotalAllocatedBytes(true) - allocated;
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new { confirmed, all }, json);
            if (!bytes.AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(captureDirectory, name + "-projection.json"))))
                throw new InvalidOperationException("Baseline or candidate equivalence failed.");
            Print(new { Phase = "paired-projection", Workspace = name, Run = run, Before = before, Milliseconds = elapsed, AllocatedBytes = bytesAllocated, Equivalent = true });
        }
    }
    context.Unload();
    return;
}
if (args[0] is "replay" or "api" or "api-screen" or "workbook")
{
    foreach (var file in Directory.GetFiles(captureDirectory, "*-inputs.json"))
    {
        var capture = JsonSerializer.Deserialize<Capture>(File.ReadAllText(file), json)!;
        var name = Path.GetFileName(file).Replace("-inputs.json", "");
        if (args[0] == "workbook") CompareWorkbook(capture, name);
        else if (args[0].StartsWith("api")) MeasureApi(capture, name); else Replay(capture, name);
    }
    return;
}
var options = new QadConnectionOptions
{
    Server = Environment.GetEnvironmentVariable("KST_PERF_QAD_SERVER"),
    Database = "QADPRO2", ConnectTimeoutSeconds = 10, CommandTimeoutSeconds = 60
};
if (!options.IsConfigured) throw new InvalidOperationException("Set KST_PERF_QAD_SERVER locally.");
var workspacesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KST", "config", "workspaces.json");
var workspaces = JsonSerializer.Deserialize<List<WorkspaceAssignment>>(File.ReadAllText(workspacesPath), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
var resolver = new QadMpsScopeResolver(options, NullLogger<QadMpsScopeResolver>.Instance);
using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(20));
if (args[0] == "metadata")
{
    await using var connection = await QadConnectionFactory.OpenAsync(options, timeout.Token);
    var columns = await connection.QueryAsync("""
        SELECT t.name AS TableName, c.name AS ColumnName, ty.name AS TypeName, c.max_length AS MaxLength
        FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
        WHERE (t.name='mrp_det' AND c.name IN ('mrp_part','mrp_domain','mrp_site','mrp_line','mrp_line2'))
           OR (t.name='pod_det' AND c.name IN ('pod_part','pod_domain','pod_site','pod_line'));
        """);
    Print(columns);
    return;
}
if (args[0] is "batch" or "compare")
{
    var capture = JsonSerializer.Deserialize<Capture>(File.ReadAllText(Path.Combine(captureDirectory, "2140-inputs.json")), json)!;
    var parts = capture.Acquisition.Inputs.Select(i => i.ComponentPart).ToArray();
    await using var connection = await QadConnectionFactory.OpenAsync(options, timeout.Token);
    connection.StatisticsEnabled = true;
    var messages = new List<string>();
    connection.InfoMessage += (_, e) => messages.Add(e.Message);
    await connection.ExecuteAsync("SET STATISTICS IO ON; SET STATISTICS TIME ON;");
    foreach (var size in args[0] == "compare" ? new[] { 250, 500, 250 } : new[] { 100, 250, 100, 250 })
    {
        messages.Clear(); connection.ResetStatistics();
        var watch = Stopwatch.StartNew();
        var count = 0;
        var raw = new List<object>();
        foreach (var batch in parts.Chunk(size))
        {
            var query = QadLongTermShortageSourceReader.BuildBatchQuery("KTC", capture.Site, batch, LongTermShortagesBuilder.GetWeekOneStart(capture.AsOf).AddDays(182));
            var rows = (await connection.QueryAsync(new CommandDefinition(query.Sql, query.Parameters, commandTimeout: 60, cancellationToken: timeout.Token))).ToList();
            count += rows.Count;
            raw.AddRange(rows);
        }
        var stats = connection.RetrieveStatistics();
        var result = new { Phase = "batch", Size = size, ElapsedMs = watch.Elapsed.TotalMilliseconds, Rows = count,
            Bytes = stats["BytesReceived"], RoundTrips = stats["ServerRoundtrips"], Messages = messages.ToArray() };
        File.AppendAllText(Path.Combine(captureDirectory, "batch.jsonl"), JsonSerializer.Serialize(result, json) + Environment.NewLine);
        File.WriteAllText(Path.Combine(captureDirectory, $"batch-{size}-{DateTime.UtcNow:HHmmss}-raw.json"), JsonSerializer.Serialize(raw, json));
        Print(new { result.Phase, result.Size, result.ElapsedMs, result.Rows, result.Bytes, result.RoundTrips });
    }
    return;
}
foreach (var workspace in workspaces.Where(w => w.IsEnabled && (args[0] == "discover" || w.ProductLineFrom is "2140" or "2141" or "2142")))
{
    var timer = Stopwatch.StartNew();
    try
    {
        var parents = await resolver.ResolveAsync(workspace, timeout.Token);
        Console.WriteLine(JsonSerializer.Serialize(new { workspace.DisplayName, Parents = parents.Count, Milliseconds = timer.Elapsed.TotalMilliseconds }));
        if (args[0] == "discover") continue;
        var name = workspace.ProductLineFrom!;
        var asOf = DateOnly.FromDateTime(DateTime.Today);
        if (args[0] is "sql" or "sql-after" or "plans")
        {
            var capture = JsonSerializer.Deserialize<Capture>(File.ReadAllText(Path.Combine(captureDirectory, name + "-inputs.json")), json)!;
            await AuditSql(capture, name);
            continue;
        }
        for (var run = 0; run < 2; run++)
        {
            timer.Restart();
            var bomReader = new QadBomReader(options, NullLogger<QadBomReader>.Instance);
            var components = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var bomTimes = new List<double>();
            foreach (var parent in parents)
            {
                var watch = Stopwatch.StartNew();
                var occurrences = await bomReader.ReadAsync(workspace.Site, parent.ParentPart, asOf, timeout.Token);
                bomTimes.Add(watch.Elapsed.TotalMilliseconds);
                foreach (var occurrence in occurrences.Where(o => !o.IsPhantom && string.Equals(o.PmCode?.Trim(), "P", StringComparison.OrdinalIgnoreCase)))
                {
                    if (!components.TryGetValue(occurrence.ComponentPart, out var users)) components[occurrence.ComponentPart] = users = new(StringComparer.OrdinalIgnoreCase);
                    users.Add(parent.ParentPart);
                }
            }
            var universeMs = timer.Elapsed.TotalMilliseconds;
            var componentParents = components.ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Value.Order(StringComparer.OrdinalIgnoreCase).ToList(), StringComparer.OrdinalIgnoreCase);
            var source = new QadLongTermShortageSourceReader(options, NullLogger<QadLongTermShortageSourceReader>.Instance);
            var sourceWatch = Stopwatch.StartNew();
            var acquisition = await source.ReadAsync(workspace.Site, componentParents, asOf, LongTermShortagesBuilder.GetWeekOneStart(asOf).AddDays(26 * 7), timeout.Token);
            var sourceMs = sourceWatch.Elapsed.TotalMilliseconds;
            var capture = new Capture(asOf, workspace.Site, parents.Select(p => p.ParentPart).ToArray(), acquisition);
            var prefix = name + (args[0] == "capture-after" ? "-after" : "") + (run == 0 ? "" : "-repeat");
            File.WriteAllText(Path.Combine(captureDirectory, prefix + "-inputs.json"), JsonSerializer.Serialize(capture, json));
            Print(new { Phase = "acquisition", Workspace = name, Run = run, Parents = parents.Count, Components = components.Count,
                Events = acquisition.Inputs.Sum(i => i.Evidence.Count), UniverseMs = universeMs, BomMs = bomTimes,
                SourceMs = sourceMs, TotalAcquisitionMs = universeMs + sourceMs });
            Replay(capture, prefix);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { workspace.DisplayName, FailureType = ex.GetType().Name, Milliseconds = timer.Elapsed.TotalMilliseconds }));
        return;
    }
}

void Print(object value)
{
    var line = JsonSerializer.Serialize(value, json);
    Console.WriteLine(line);
    File.AppendAllText(Path.Combine(captureDirectory, "measurements.jsonl"), line + Environment.NewLine);
}

void CompareWorkbook(Capture capture, string name)
{
    if (name.Contains("repeat")) return;
    using var baseline = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(captureDirectory, name + "-projection.json")));
    foreach (var mode in new[] { "confirmed", "all" })
    {
        var oldRows = baseline.RootElement.GetProperty(mode).Deserialize<List<LongTermShortageRow>>(json)!;
        var newRows = LongTermShortagesBuilder.Build(capture.AsOf, capture.Acquisition.Inputs, includeUnconfirmed: mode == "all");
        var exporter = new Kst.Exports.PlaceholderExportService();
        string Hash(IReadOnlyList<LongTermShortageRow> rows)
        {
            var bytes = exporter.CreateLongTermShortagesWorkbook(name, capture.AsOf, rows, capture.Acquisition.AcquiredAtUtc, LongTermShortageAcquisition.ConsistencyMode);
            using var stream = new MemoryStream(bytes);
            using var book = new ClosedXML.Excel.XLWorkbook(stream);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var sheet in book.Worksheets)
            foreach (var cell in sheet.CellsUsed())
                hash.AppendData(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { sheet.Name, Address = cell.Address.ToString(), Type = cell.DataType.ToString(), Value = cell.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), Style = cell.Style.ToString() }) + "\n"));
            return Convert.ToHexString(hash.GetHashAndReset());
        }
        var before = Hash(oldRows);
        var after = Hash(newRows);
        if (before != after) throw new InvalidOperationException("Workbook mismatch: " + name);
        Print(new { Phase = "workbook-equivalence", Workspace = name, Mode = mode, Equal = true, Hash = after });
    }
}

void MeasureApi(Capture capture, string name)
{
    var confirmed = LongTermShortagesBuilder.Build(capture.AsOf, capture.Acquisition.Inputs);
    var all = LongTermShortagesBuilder.Build(capture.AsOf, capture.Acquisition.Inputs, includeUnconfirmed: true);
    var map = typeof(LongTermShortagesEndpoints).GetMethod("MapForResponse", BindingFlags.NonPublic | BindingFlags.Static)!;
    var mapper = map.CreateDelegate<Func<LongTermShortageRow, bool, LongTermShortageRowDto>>();
    var includeEvidence = args[0] != "api-screen";
    for (var run = 0; run < 6; run++)
    {
        var timer = Stopwatch.StartNew();
        var dto = new LongTermShortagesResponseDto("00000000-0000-0000-0000-000000000001", capture.AsOf, false, null,
            confirmed.Select(row => mapper(row, includeEvidence)).ToList(), capture.Acquisition.AcquiredAtUtc, LongTermShortageAcquisition.ConsistencyMode,
            all.Select(row => mapper(row, includeEvidence)).ToList()) { EvidenceIncluded = includeEvidence };
        var mappingMs = timer.Elapsed.TotalMilliseconds;
        timer.Restart();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(dto, json);
        var serializationMs = timer.Elapsed.TotalMilliseconds;
        if (run == 0) File.WriteAllBytes(Path.Combine(captureDirectory, name + (includeEvidence ? "" : "-screen") + "-api.json"), bytes);
        var evidenceBytes = JsonSerializer.SerializeToUtf8Bytes(dto.Rows.Select(r => r.Evidence).Concat(dto.AllReceiptsRows.Select(r => r.Evidence)), json).Length;
        Print(new { Phase = "api", Workspace = name, Run = run, MappingMs = mappingMs, SerializationMs = serializationMs,
            ResponseBytes = bytes.Length, EvidenceBytes = evidenceBytes });
    }
}

void Replay(Capture capture, string name)
{
    for (var run = 0; run < 6; run++)
    {
        var allocated = GC.GetTotalAllocatedBytes(true);
        var timer = Stopwatch.StartNew();
        var confirmed = LongTermShortagesBuilder.Build(capture.AsOf, capture.Acquisition.Inputs);
        var all = LongTermShortagesBuilder.Build(capture.AsOf, capture.Acquisition.Inputs, includeUnconfirmed: true);
        var projectionMs = timer.Elapsed.TotalMilliseconds;
        var projectionBytes = GC.GetTotalAllocatedBytes(true) - allocated;
        timer.Restart();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { confirmed, all }, json);
        var serializeMs = timer.Elapsed.TotalMilliseconds;
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        if (run == 0)
        {
            var baselinePath = Path.Combine(captureDirectory, name + "-projection.json");
            if (File.Exists(baselinePath) && !File.ReadAllBytes(baselinePath).AsSpan().SequenceEqual(bytes))
                throw new InvalidOperationException("Projection differs from captured baseline: " + name);
            if (!File.Exists(baselinePath)) File.WriteAllBytes(baselinePath, bytes);
        }
        Print(new { Phase = "projection-replay", Workspace = name, Run = run, ProjectionMs = projectionMs,
            AllocatedBytes = projectionBytes, DomainSerializationMs = serializeMs, DomainBytes = bytes.Length, Hash = hash,
            WorkingSet = Environment.WorkingSet, Short = confirmed.Count(r => r.HasShortage) });
    }
}

async Task AuditSql(Capture capture, string name)
{
    var commands = new List<(string Name, string Sql, DynamicParameters Parameters)>();
    foreach (var parent in capture.Parents)
    {
        var query = QadBomReader.BuildQuery("KTC", capture.Site, parent, capture.AsOf);
        commands.Add(($"bom-{commands.Count}", query.Sql, query.Parameters));
    }
    var parts = capture.Acquisition.Inputs.Select(i => i.ComponentPart).ToArray();
    foreach (var batch in parts.Chunk(args[0] == "sql-after" ? QadLongTermShortageSourceReader.MaxSourceBatchSize : 500))
    {
        var facts = QadLongTermShortageSourceReader.BuildBatchQuery("KTC", capture.Site, batch, LongTermShortagesBuilder.GetWeekOneStart(capture.AsOf).AddDays(182));
        commands.Add(("facts", facts.Sql, facts.Parameters));
    }
    foreach (var batch in parts.Chunk(500))
    {
        var presentation = QadLongTermShortageSourceReader.BuildPresentationQuery("KTC", capture.Site, batch, capture.AsOf);
        commands.Add(("presentation", presentation.Sql, presentation.Parameters));
    }
    var drawer = QadComponentOrderReader.BuildBatchQuery("KTC", capture.Site, [parts[0]], capture.AsOf);
    commands.Add(("drawer-po", drawer.Sql, drawer.Parameters));
    await using var connection = await QadConnectionFactory.OpenAsync(options, timeout.Token);
    connection.StatisticsEnabled = true;
    var messages = new List<string>();
    connection.InfoMessage += (_, e) => messages.Add(e.Message);
    await connection.ExecuteAsync("SET STATISTICS IO ON; SET STATISTICS TIME ON;");
    for (var run = 0; run < (args[0] == "plans" ? 0 : 2); run++)
    foreach (var command in commands)
    {
        messages.Clear();
        connection.ResetStatistics();
        var watch = Stopwatch.StartNew();
        var rows = (await connection.QueryAsync(new CommandDefinition(command.Sql, command.Parameters, commandTimeout: 60, cancellationToken: timeout.Token))).ToList();
        var elapsed = watch.Elapsed.TotalMilliseconds;
        var statistics = connection.RetrieveStatistics();
        var result = new { Phase = "sql", Workspace = name, Run = run, Query = command.Name, ElapsedMs = elapsed,
            Rows = rows.Count, BytesReceived = statistics["BytesReceived"], RoundTrips = statistics["ServerRoundtrips"], Messages = messages.ToArray() };
        Print(result);
        File.AppendAllText(Path.Combine(captureDirectory, name + (args[0] == "sql-after" ? "-after" : "") + "-sql.jsonl"), JsonSerializer.Serialize(result, json) + Environment.NewLine);
    }
    await connection.ExecuteAsync("SET STATISTICS IO OFF; SET STATISTICS TIME OFF;");
    // SHOWPLAN permission may be unavailable. Never broaden permissions to obtain it.
    try
    {
        await using var setting = connection.CreateCommand();
        setting.CommandText = "SET SHOWPLAN_XML ON;";
        await setting.ExecuteNonQueryAsync(timeout.Token);
        var index = 0;
        foreach (var command in commands.Where(c => c.Name is "facts" or "presentation" or "drawer-po"))
        {
            // SHOWPLAN over parameterized RPC calls can return no plan. Compile the same SQL
            // with local declarations; record that estimates are local-variable estimates.
            await using var planCommand = connection.CreateCommand();
            planCommand.CommandTimeout = 60;
            var declarations = command.Parameters.ParameterNames.Select(parameter =>
            {
                var value = command.Parameters.Get<object>(parameter);
                return value is DateTime date
                    ? $"DECLARE @{parameter} date = '{date:yyyyMMdd}';"
                    : $"DECLARE @{parameter} nvarchar(4000) = N'{value?.ToString()?.Replace("'", "''")}';";
            });
            planCommand.CommandText = string.Join(Environment.NewLine, declarations) + Environment.NewLine + command.Sql;
            using var reader = await planCommand.ExecuteReaderAsync(timeout.Token);
            var plans = new List<string>();
            do
            {
                Print(new { Phase = "plan-shape", Fields = reader.FieldCount, Types = Enumerable.Range(0, reader.FieldCount).Select(reader.GetDataTypeName).ToArray() });
                while (reader.Read())
                {
                    var value = reader.GetValue(0);
                    var xml = value is System.Data.SqlTypes.SqlXml sqlXml ? sqlXml.Value : value.ToString();
                    if (reader.FieldCount == 1 && xml?.Contains("ShowPlanXML") == true) plans.Add(xml);
                }
            } while (reader.NextResult());
            File.WriteAllText(Path.Combine(captureDirectory, name + "-" + command.Name + "-" + index++ + ".sqlplan"), string.Join(Environment.NewLine, plans));
            Print(new { Phase = "plan-query", Workspace = name, Query = command.Name, Plans = plans.Count });
        }
    }
    catch (SqlException ex) { Print(new { Phase = "plans", Workspace = name, Available = false, SqlError = ex.Number }); }
    finally { await connection.ExecuteAsync("SET SHOWPLAN_XML OFF;"); }
}

record Capture(DateOnly AsOf, string Site, IReadOnlyList<string> Parents, LongTermShortageAcquisition Acquisition);

static class NormalizationReplay
{
    public static void Measure<T>(string rawJson, Capture capture, JsonSerializerOptions json)
    {
        // Dynamic SQL capture retains MAX(CAST(bit AS int)); mirror Dapper's 0/1-to-bool
        // materialization before replaying the production normalization boundary.
        var rawNodes = System.Text.Json.Nodes.JsonNode.Parse(rawJson)!.AsArray();
        foreach (var row in rawNodes)
            if (row?["PoConfirmed"] is System.Text.Json.Nodes.JsonValue value && value.TryGetValue<int>(out var flag))
                row["PoConfirmed"] = flag != 0;
        var rows = rawNodes.Deserialize<List<T>>(json)!;
        var partProperty = typeof(T).GetProperty("ComponentPart")!;
        var method = typeof(QadLongTermShortageSourceReader).GetMethod("ToInput", BindingFlags.NonPublic | BindingFlags.Static)!;
        var parents = capture.Acquisition.Inputs.ToDictionary(i => i.ComponentPart, i => i.DemandParentParts, StringComparer.OrdinalIgnoreCase);
        var presentation = capture.Acquisition.Inputs.ToDictionary(i => i.ComponentPart, i => i.Presentation!, StringComparer.OrdinalIgnoreCase);
        byte[]? baseline = null;
        for (var run = 0; run < 6; run++)
        {
            var timer = Stopwatch.StartNew();
            var groups = rows.GroupBy(row => (string)partProperty.GetValue(row)!, StringComparer.OrdinalIgnoreCase).ToList();
            var groupingMs = timer.Elapsed.TotalMilliseconds;
            timer.Restart();
            var normalized = groups.Select(group => (LongTermShortageInput)method.Invoke(null, [group, parents, presentation])!).ToList();
            var normalizeMs = timer.Elapsed.TotalMilliseconds;
            var bytes = JsonSerializer.SerializeToUtf8Bytes(normalized.OrderBy(i => i.ComponentPart), json);
            baseline ??= bytes;
            if (!baseline.AsSpan().SequenceEqual(bytes)) throw new InvalidOperationException("Normalization mismatch.");
            // Repartition the same raw component groups; all rows for a component stay together.
            var partitioned = groups.Chunk(250).SelectMany(batch => batch.Select(group => (LongTermShortageInput)method.Invoke(null, [group, parents, presentation])!)).ToList();
            if (!bytes.AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(partitioned.OrderBy(i => i.ComponentPart), json)))
                throw new InvalidOperationException("Batch partition changed normalized inputs.");
            Console.WriteLine(JsonSerializer.Serialize(new { Phase = "normalization", Run = run, RawRows = rows.Count,
                Components = groups.Count, GroupingMs = groupingMs, CombinedInventoryMrpPoNormalizationMs = normalizeMs, PartitionEquivalent = true }, json));
        }
    }
}

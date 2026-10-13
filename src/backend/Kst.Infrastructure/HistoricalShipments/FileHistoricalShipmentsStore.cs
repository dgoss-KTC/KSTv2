using System.Runtime.CompilerServices;
using System.Text.Json;
using Kst.Application.HistoricalShipments;
using Kst.Domain.HistoricalShipments;

namespace Kst.Infrastructure.HistoricalShipments;

/// <summary>Private process-root files; no caller paths. Bounded slots/pages; disk-backed row index.</summary>
public sealed class FileHistoricalShipmentsStore : IHistoricalShipmentsStore, IDisposable
{
    private sealed class Entry(Guid id, string owner, string directory, DateTimeOffset expires)
    {
        public readonly Guid Id = id;
        public readonly string Owner = owner, Directory = directory;
        public readonly DateTimeOffset Expires = expires;
        public readonly CancellationTokenSource Cancel = new();
        public readonly SemaphoreSlim Gate = new(1, 1);
        public int Leases;
        public bool Released;
        public ShipmentOperation Operation = new(id, "registered", 0, [], expires, null, 0);
        public string Rows => Path.Combine(Directory, "rows.bin");
        public string Index => Path.Combine(Directory, "index.bin");
        public string Workbook => Path.Combine(Directory, "workbook.xlsx");
        public string PendingWorkbook => Path.Combine(Directory, "workbook.pending");
    }
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Entry> _entries = [];
    private readonly HashSet<Guid> _used = [];
    private readonly List<Entry> _cleanupPending = [];
    private readonly string _root;
    private readonly TimeProvider _clock;
    private readonly TimeSpan _lifetime;
    private readonly ITimer _timer;
    private readonly SemaphoreSlim _workers = new(2, 2);
    private bool _disposed;
    public FileHistoricalShipmentsStore(string root, TimeProvider? clock = null, TimeSpan? lifetime = null)
    {
        _root = Path.Combine(root, Guid.NewGuid().ToString("N"));
        _clock = clock ?? TimeProvider.System; _lifetime = lifetime ?? TimeSpan.FromMinutes(30);
        Directory.CreateDirectory(_root);
        SweepAbandonedRoots(root, _root); _timer = _clock.CreateTimer(_ => Prune(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }
    /// <summary>Best-effort removal of store roots abandoned by a killed process (older than two hours).</summary>
    private static void SweepAbandonedRoots(string parent, string current)
    {
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(parent))
            {
                var name = Path.GetFileName(dir);
                if (dir == current || name.Length != 32 || !Guid.TryParseExact(name, "N", out _)) continue;
                if (DateTime.UtcNow - Directory.GetLastWriteTimeUtc(dir) < TimeSpan.FromHours(2)) continue;
                try { Directory.Delete(dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
    public ShipmentOperation Register(Guid id, string owner)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this); Prune();
            if (id == Guid.Empty) throw new ArgumentException("Operation ID must not be empty.");
            if (_used.Contains(id)) throw new ShipmentOperationException(409, "Operation IDs cannot be reused. Start a new operation.");
            if (_entries.Count >= 8 || _used.Count >= 4096) throw new ShipmentOperationException(503, "Operation capacity reached. Release results or restart after reviewing save outcomes.");
            var directory = Path.Combine(_root, id.ToString("N")); Directory.CreateDirectory(directory);
            var e = new Entry(id, owner, directory, _clock.GetUtcNow() + _lifetime);
            _entries.Add(id, e); _used.Add(id); return e.Operation;
        }
    }
    private Entry Lease(Guid id, string owner)
    {
        lock (_gate)
        {
            Prune();
            if (!_entries.TryGetValue(id, out var e) || e.Owner != owner || e.Released)
                throw new ShipmentOperationException(410, "Operation expired, released or not owned by this report session.");
            e.Leases++; return e;
        }
    }
    private void Unlease(Entry e)
    {
        lock (_gate) { e.Leases--; if (e.Released && e.Leases == 0) Cleanup(e); }
    }
    public ShipmentOperation Get(Guid id, string owner)
    {
        var e = Lease(id, owner); try { lock (_gate) return e.Operation; } finally { Unlease(e); }
    }
    public async Task<ShipmentOperation> AcquireAsync(Guid id, string owner, HistoricalShipmentsFilter filter,
        Func<CancellationToken, IAsyncEnumerable<ShipmentRow>> read, CancellationToken ct, bool includeOnTimeSummary = false)
    {
        var e = Lease(id, owner); var worker = false; var acquired = false;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, e.Cancel.Token);
        try
        {
            if (!await e.Gate.WaitAsync(0, linked.Token)) throw new ShipmentOperationException(409, "Operation is busy.");
            acquired = true;
            if (e.Operation.State != "registered") throw new ShipmentOperationException(409, "Acquisition already started. Use a new operation.");
            worker = await _workers.WaitAsync(0, linked.Token);
            if (!worker) throw new ShipmentOperationException(503, "Report acquisition/preparation is busy. Retry.");
            lock (_gate) e.Operation = e.Operation with { State = "acquiring", Columns = filter.Columns };
            using var rows = new FileStream(e.Rows, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var index = new BinaryWriter(new FileStream(e.Index, FileMode.CreateNew, FileAccess.Write, FileShare.None));
            using var writer = new BinaryWriter(rows);
            var projection = filter.Columns.Select(id => HistoricalShipmentsRules.Columns.ToList().FindIndex(c => c.Id == id)).ToArray();
            long count = 0, unavailable = 0, onTimeNumerator = 0, onTimeDenominator = 0;
            var onTimeIndex = includeOnTimeSummary
                ? HistoricalShipmentsRules.Columns.ToList().FindIndex(column => column.Id == "onTime")
                : -1;
            if (includeOnTimeSummary && onTimeIndex < 0)
                throw new InvalidOperationException("On Time summary source column is unavailable.");
            await foreach (var row in read(linked.Token).WithCancellation(linked.Token))
            {
                if (row.Cells.Count != HistoricalShipmentsRules.Columns.Count || string.IsNullOrEmpty(row.PhysicalDetailId))
                    throw new ShipmentSourceException("Invalid invoice detail schema. No complete report was acquired.");
                if (includeOnTimeSummary && row.Cells[onTimeIndex] is { } onTime && (onTime == "1" || onTime == "0"))
                {
                    onTimeDenominator++;
                    if (onTime == "1") onTimeNumerator++;
                }
                var selected = new ShipmentRow(row.PhysicalDetailId, projection.Select(i => row.Cells[i]).ToArray(), row.UnavailableEnrichment);
                var bytes = JsonSerializer.SerializeToUtf8Bytes(selected);
                index.Write(rows.Position); writer.Write(bytes.Length); writer.Write(bytes);
                count++; if (row.UnavailableEnrichment) unavailable++;
            }
            writer.Flush(); index.Flush();
            lock (_gate)
            {
                linked.Token.ThrowIfCancellationRequested();
                e.Operation = e.Operation with
                {
                    State = "ready",
                    RowCount = count,
                    AcquiredAtUtc = _clock.GetUtcNow(),
                    UnavailableEnrichmentRows = unavailable,
                    OnTimeNumerator = includeOnTimeSummary ? onTimeNumerator : null,
                    OnTimeDenominator = includeOnTimeSummary ? onTimeDenominator : null
                };
                return e.Operation;
            }
        }
        catch { if (worker) Release(id, owner); throw; }
        finally { if (worker) _workers.Release(); if (acquired) e.Gate.Release(); Unlease(e); }
    }
    private static ShipmentRow ReadRow(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        // Bound a single row, not the complete history. Source cell capacities are explicit.
        if (length < 0 || length > 16 * 1024 * 1024) throw new InvalidDataException("Frozen row is invalid.");
        var bytes = reader.ReadBytes(length);
        if (bytes.Length != length) throw new InvalidDataException("Frozen result is incomplete.");
        return JsonSerializer.Deserialize<ShipmentRow>(bytes) ?? throw new InvalidDataException();
    }
    private static FileStream OpenRead(string path) => new(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
    public async Task<ShipmentPage> PageAsync(Guid id, string owner, long offset, int limit, CancellationToken ct)
    {
        if (offset < 0 || limit < 1 || limit > HistoricalShipmentsRules.MaximumPageSize) throw new ArgumentException("Invalid page offset/limit.");
        var e = Lease(id, owner);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, e.Cancel.Token);
        try
        {
            await e.Gate.WaitAsync(linked.Token);
            try
            {
                if (e.Operation.State != "ready") throw new ShipmentOperationException(409, "Complete report is not ready.");
                var result = new List<ShipmentRow>();
                if (offset < e.Operation.RowCount)
                {
                    using var index = new BinaryReader(OpenRead(e.Index)); index.BaseStream.Position = checked(offset * sizeof(long));
                    using var reader = new BinaryReader(OpenRead(e.Rows)); reader.BaseStream.Position = index.ReadInt64();
                    for (var i = offset; i < e.Operation.RowCount && result.Count < limit; i++)
                    { linked.Token.ThrowIfCancellationRequested(); result.Add(ReadRow(reader)); }
                }
                linked.Token.ThrowIfCancellationRequested(); return new(e.Operation, offset, result.AsReadOnly());
            }
            finally { e.Gate.Release(); }
        }
        finally { Unlease(e); }
    }
    private static async IAsyncEnumerable<ShipmentRow> ReadRows(Entry e, [EnumeratorCancellation] CancellationToken ct)
    {
        using var reader = new BinaryReader(OpenRead(e.Rows));
        for (long i = 0; i < e.Operation.RowCount; i++)
        {
            ct.ThrowIfCancellationRequested(); yield return ReadRow(reader);
            if (i % 256 == 0) await Task.Yield();
        }
        if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Frozen row count mismatch.");
    }
    public async Task<ShipmentPreparation> PrepareAsync(Guid id, string owner,
        Func<IAsyncEnumerable<ShipmentRow>, IReadOnlyList<string>, Stream, CancellationToken, Task> write, CancellationToken ct)
    {
        var e = Lease(id, owner); var entered = false; var worker = false;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, e.Cancel.Token);
        try
        {
            if (!await e.Gate.WaitAsync(0, linked.Token)) throw new ShipmentOperationException(409, "Operation is busy.");
            entered = true;
            if (e.Operation.State != "ready") throw new ShipmentOperationException(409, "Complete report is not ready.");
            if (File.Exists(e.Workbook)) return new(id, new FileInfo(e.Workbook).Length, e.Expires);
            if (e.Operation.RowCount > HistoricalShipmentsRules.MaximumXlsxDataRows)
                throw new ShipmentOperationException(422, "XLSX supports at most 1,048,575 detail rows plus its header. No workbook was prepared; the full result remains available for display.");
            worker = await _workers.WaitAsync(0, linked.Token);
            if (!worker) throw new ShipmentOperationException(503, "Report acquisition/preparation is busy. Retry.");
            using (var output = new FileStream(e.PendingWorkbook, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await write(ReadRows(e, linked.Token), e.Operation.Columns, output, linked.Token); await output.FlushAsync(linked.Token); }
            lock (_gate)
            {
                linked.Token.ThrowIfCancellationRequested();
                File.Move(e.PendingWorkbook, e.Workbook);
                return new(id, new FileInfo(e.Workbook).Length, e.Expires);
            }
        }
        catch { if (worker) Release(id, owner); throw; }
        finally { if (worker) _workers.Release(); if (entered) e.Gate.Release(); Unlease(e); }
    }
    public Stream OpenPreparation(Guid id, string owner)
    {
        var e = Lease(id, owner);
        try
        {
            lock (_gate)
            {
                if (e.Released || !File.Exists(e.Workbook)) throw new ShipmentOperationException(410, "Complete preparation unavailable.");
                return OpenRead(e.Workbook);
            }
        }
        finally { Unlease(e); }
    }
    public void Release(Guid id, string owner)
    {
        lock (_gate)
        {
            if (_entries.TryGetValue(id, out var e)) { if (e.Owner == owner) Release(e); }
            // A cancellation can arrive before registration. Never let a late registration resurrect it.
            else if (_used.Count < 4096) _used.Add(id);
        }
    }
    private void Release(Entry e)
    {
        _entries.Remove(e.Id); e.Released = true; e.Cancel.Cancel(); if (e.Leases == 0) Cleanup(e);
    }
    private void Cleanup(Entry e)
    {
        try
        {
            if (Directory.Exists(e.Directory)) Directory.Delete(e.Directory, true);
            _cleanupPending.Remove(e); e.Cancel.Dispose();
        }
        catch (IOException) { if (!_cleanupPending.Contains(e)) _cleanupPending.Add(e); }
        catch (UnauthorizedAccessException) { if (!_cleanupPending.Contains(e)) _cleanupPending.Add(e); }
    }
    private void Prune()
    {
        lock (_gate)
        {
            foreach (var e in _entries.Values.Where(e => e.Expires <= _clock.GetUtcNow()).ToArray()) Release(e);
            foreach (var e in _cleanupPending.ToArray()) Cleanup(e);
        }
    }
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return; _disposed = true; _timer.Dispose();
            foreach (var e in _entries.Values.ToArray()) Release(e);
            if (Directory.Exists(_root) && !Directory.EnumerateFileSystemEntries(_root).Any()) Directory.Delete(_root);
        }
    }
}

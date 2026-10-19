using Kst.Application.HistoricalShipments;
using Kst.Domain.HistoricalShipments;

namespace Kst.Infrastructure.HistoricalShipments;

/// <summary>External ordinal sort: <=4096 strings/1MiB per run, 16 merge readers, top 1000.
/// Disk scratch is linear in the acquired column, never an unbounded distinct-value dictionary.
/// Online base-16 compaction bounds the run manifest/files to 16 levels of at most 15 runs
/// (plus one merge's input/output), even for a long-sized row population.</summary>
internal sealed class ShipmentFacetSort : IDisposable
{
    private readonly string _directory;
    private readonly List<string> _chunk = [];
    private int _chunkBytes;
    private long _nextFile;
    private readonly List<string>[] _levels = Enumerable.Range(0, 16).Select(_ => new List<string>(16)).ToArray();
    public ShipmentFacetSort(string directory)
    {
        _directory = directory;
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
        Directory.CreateDirectory(directory);
    }
    private string NewRun() => Path.Combine(_directory, $"run-{_nextFile++}.bin");
    public async Task AddAsync(string value, CancellationToken ct)
    {
        _chunk.Add(value); _chunkBytes += value.Length * sizeof(char);
        if (_chunk.Count >= 4096 || _chunkBytes >= 1024 * 1024) await FlushAsync(ct);
    }
    private async Task FlushAsync(CancellationToken ct)
    {
        if (_chunk.Count == 0) return;
        ct.ThrowIfCancellationRequested();
        _chunk.Sort(StringComparer.Ordinal);
        var incoming = NewRun();
        using (var writer = new BinaryWriter(File.Create(incoming)))
        {
            for (var i = 0; i < _chunk.Count;)
            {
                var value = _chunk[i]; long count = 0;
                do { count++; i++; } while (i < _chunk.Count && _chunk[i] == value);
                writer.Write(value); writer.Write(count);
            }
        }
        _chunk.Clear(); _chunkBytes = 0;
        for (var level = 0; level < _levels.Length; level++)
        {
            var runs = _levels[level]; runs.Add(incoming);
            if (runs.Count < 16) return;
            var merged = NewRun(); await MergeAsync(runs, merged, ct);
            foreach (var path in runs) File.Delete(path);
            runs.Clear(); incoming = merged;
        }
        throw new InvalidDataException("Facet run capacity exceeded.");
    }
    private static async Task MergeAsync(IReadOnlyList<string> paths, string output, CancellationToken ct)
    {
        var readers = new List<BinaryReader>(16);
        var queue = new PriorityQueue<(string Value, long Count, int Reader), string>(StringComparer.Ordinal);
        try
        {
            void Next(int i)
            {
                var r = readers[i];
                if (r.BaseStream.Position < r.BaseStream.Length)
                { var value = r.ReadString(); queue.Enqueue((value, r.ReadInt64(), i), value); }
            }
            foreach (var path in paths) { readers.Add(new BinaryReader(File.OpenRead(path))); Next(readers.Count - 1); }
            using var writer = new BinaryWriter(File.Create(output));
            long visited = 0;
            while (queue.TryDequeue(out var item, out _))
            {
                ct.ThrowIfCancellationRequested(); var value = item.Value; var count = item.Count; Next(item.Reader);
                while (queue.TryPeek(out var next, out _) && next.Value == value)
                { queue.Dequeue(); count += next.Count; Next(next.Reader); }
                writer.Write(value); writer.Write(count);
                if (++visited % 256 == 0) await Task.Yield();
            }
        }
        finally { foreach (var r in readers) r.Dispose(); }
    }
    public async Task<ShipmentFacets> CompleteAsync(Guid id, long version, string column, string? search,
        HashSet<string> retained, long blanks, CancellationToken ct)
    {
        await FlushAsync(ct);
        var remaining = _levels.SelectMany(runs => runs).ToList(); // <= 240 paths, independent of history
        while (remaining.Count > 1)
        {
            var next = new List<string>(16);
            for (var start = 0; start < remaining.Count; start += 16)
            {
                ct.ThrowIfCancellationRequested();
                var paths = remaining.Skip(start).Take(16).ToArray(); var merged = NewRun();
                await MergeAsync(paths, merged, ct); next.Add(merged);
                foreach (var path in paths) File.Delete(path);
            }
            remaining = next;
        }
        var top = new SortedSet<ShipmentFacetValue>(Comparer<ShipmentFacetValue>.Create((a, b) =>
        { var count = b.Count.CompareTo(a.Count); return count != 0 ? count : string.CompareOrdinal(a.Value, b.Value); }));
        var selected = retained.ToDictionary(v => v, _ => 0L, StringComparer.Ordinal);
        long distinct = 0, matching = 0;
        if (remaining.Count > 0)
        {
            using var reader = new BinaryReader(File.OpenRead(remaining[0]));
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                ct.ThrowIfCancellationRequested();
                var value = reader.ReadString(); var count = reader.ReadInt64(); distinct++;
                if (selected.ContainsKey(value)) selected[value] = count;
                if (string.IsNullOrEmpty(search) || value.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    matching++; top.Add(new(value, count));
                    if (top.Count > ShipmentViewRules.MaximumValues) top.Remove(top.Max!);
                }
                if (distinct % 256 == 0) await Task.Yield();
            }
        }
        return new(id, version, column, top.ToArray(), selected.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new ShipmentFacetValue(p.Key, p.Value)).ToArray(),
            blanks, distinct, matching, matching > ShipmentViewRules.MaximumValues);
    }
    public void Dispose()
    {
        try { Directory.Delete(_directory, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}

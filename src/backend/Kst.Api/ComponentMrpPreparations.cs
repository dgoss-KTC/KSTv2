using System.Security.Cryptography;

namespace Kst.Api;

/// <summary>Process-local bounded transport cache; never reacquires report facts on read.</summary>
public sealed class ComponentMrpPreparations
{
    public const int MaxBytes = 8 * 1024 * 1024;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    private sealed record Entry(string Owner, string ReportKind, byte[] Bytes, DateTimeOffset Expires);
    private readonly Dictionary<Guid, Entry> _entries = [];
    private readonly object _gate = new();
    public SemaphoreSlim Generations { get; } = new(2, 2);

    public (Guid Id, DateTimeOffset Expires) Add(string owner, byte[] bytes, string reportKind = "ComponentMrp")
    {
        if (bytes.Length > MaxBytes) throw new InvalidOperationException("Workbook exceeds preparation capacity.");
        lock (_gate)
        {
            Prune();
            // A managed host session owns only its latest preparation.
            foreach (var key in _entries.Where(p => p.Value.Owner == owner).Select(p => p.Key).ToArray()) _entries.Remove(key);
            if (_entries.Count >= 8) throw new InvalidOperationException("Preparation capacity exhausted.");
            var id = Guid.NewGuid(); var expires = DateTimeOffset.UtcNow + Lifetime;
            _entries.Add(id, new(owner, reportKind, (byte[])bytes.Clone(), expires));
            return (id, expires);
        }
    }

    public byte[]? Get(Guid id, string owner, string reportKind = "ComponentMrp")
    {
        lock (_gate)
        {
            Prune();
            return _entries.TryGetValue(id, out var entry) && entry.ReportKind == reportKind && CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(entry.Owner), System.Text.Encoding.UTF8.GetBytes(owner))
                ? (byte[])entry.Bytes.Clone() : null;
        }
    }

    private void Prune()
    {
        foreach (var key in _entries.Where(p => p.Value.Expires <= DateTimeOffset.UtcNow).Select(p => p.Key).ToArray()) _entries.Remove(key);
    }

    public void Discard(string owner, string reportKind)
    {
        lock (_gate)
        {
            foreach (var key in _entries.Where(p => p.Value.Owner == owner && p.Value.ReportKind == reportKind).Select(p => p.Key).ToArray())
                _entries.Remove(key);
        }
    }
}

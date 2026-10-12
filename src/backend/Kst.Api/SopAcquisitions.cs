using Kst.Domain.Sop;

namespace Kst.Api;

/// <summary>S&OP-only held facts. Tokens cannot reacquire or cross owner/kind boundaries.</summary>
public sealed class SopAcquisitions
{
    public sealed record Entry(string Owner, string Site, string GroupBy, SopSource Source,
        DateOnly AsOfDate, DateTimeOffset AcquiredAtUtc, DateTimeOffset Expires);
    private readonly Dictionary<Guid, Entry> _entries = [];
    private readonly object _gate = new();
    public (Guid Id, Entry Entry) Add(string owner, string site, string groupBy, SopSource source)
    {
        lock (_gate)
        {
            Prune();
            foreach (var id in _entries.Where(e => e.Value.Owner == owner).Select(e => e.Key).ToArray()) _entries.Remove(id);
            if (_entries.Count >= 8) throw new InvalidOperationException("Acquisition capacity exhausted.");
            var now = DateTimeOffset.UtcNow;
            var entry = new Entry(owner, site, groupBy, source, DateOnly.FromDateTime(DateTime.Now), now, now + ComponentMrpPreparations.Lifetime);
            var token = Guid.NewGuid(); _entries.Add(token, entry); return (token, entry);
        }
    }
    public Entry? Take(Guid id, string owner)
    {
        lock (_gate)
        {
            Prune();
            if (!_entries.TryGetValue(id, out var entry) || entry.Owner != owner) return null;
            _entries.Remove(id); return entry;
        }
    }
    public void Discard(Guid id, string owner)
    {
        lock (_gate) { Prune(); if (_entries.TryGetValue(id, out var entry) && entry.Owner == owner) _entries.Remove(id); }
    }
    public void DiscardOwner(string owner)
    {
        lock (_gate) { Prune(); foreach (var id in _entries.Where(e => e.Value.Owner == owner).Select(e => e.Key).ToArray()) _entries.Remove(id); }
    }
    private void Prune()
    {
        foreach (var id in _entries.Where(e => e.Value.Expires <= DateTimeOffset.UtcNow).Select(e => e.Key).ToArray()) _entries.Remove(id);
    }
}

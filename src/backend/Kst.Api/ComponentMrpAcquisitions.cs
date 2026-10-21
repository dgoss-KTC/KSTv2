using Kst.Application.ComponentMrp;
using Kst.Domain.Common;
using System.Text.Json;

namespace Kst.Api;

/// <summary>Screen evidence only. Independent of prepared-byte/save lifetimes; never reads a source.</summary>
public sealed class ComponentMrpAcquisitions(IClock clock)
{
    public const int MaximumOwners = 8;
    public const int MaximumFactsPerAcquisition = 100_000;
    public const int MaximumRetainedFacts = 200_000;
    public const int MaximumSourceBytes = 8 * 1024 * 1024;
    public const int MaximumRetainedSourceBytes = 16 * 1024 * 1024;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    public sealed record Entry(string Owner, ComponentMrpReport Report, DateTimeOffset Expires, int SourceBytes);
    private readonly Dictionary<Guid, Entry> _entries = [];
    private readonly object _gate = new();

    public (Guid Id, Entry Entry) Add(string owner, ComponentMrpReport report)
    {
        var count = report.Source.Input.Evidence.Count;
        if (count > MaximumFactsPerAcquisition) throw new InvalidOperationException("MRP evidence exceeds retention capacity.");
        var size = JsonSerializer.SerializeToUtf8Bytes(report.Source).Length;
        if (size > MaximumSourceBytes) throw new InvalidOperationException("MRP source exceeds retention capacity.");
        lock (_gate)
        {
            Prune();
            var others = _entries.Values.Where(e => e.Owner != owner).ToArray();
            if (others.Length >= MaximumOwners || others.Sum(e => e.Report.Source.Input.Evidence.Count) + count > MaximumRetainedFacts
                || others.Sum(e => e.SourceBytes) + size > MaximumRetainedSourceBytes)
                throw new InvalidOperationException("MRP retention capacity exhausted.");
            foreach (var id in _entries.Where(e => e.Value.Owner == owner).Select(e => e.Key).ToArray()) _entries.Remove(id);
            var entry = new Entry(owner, report, clock.UtcNow + Lifetime, size);
            var token = Guid.NewGuid(); _entries.Add(token, entry); return (token, entry);
        }
    }

    public Entry? Get(Guid id, string owner, string site, string part, int weeks, DateOnly asOf)
    {
        lock (_gate)
        {
            Prune();
            return _entries.TryGetValue(id, out var entry) && entry.Owner == owner && entry.Report.Site == site
                && entry.Report.Part == part && entry.Report.HorizonWeeks == weeks
                && entry.Report.AsOfDate == asOf && asOf == DateOnly.FromDateTime(clock.LocalNow.DateTime) ? entry : null;
        }
    }

    private void Prune()
    {
        foreach (var id in _entries.Where(e => e.Value.Expires <= clock.UtcNow).Select(e => e.Key).ToArray()) _entries.Remove(id);
    }
}

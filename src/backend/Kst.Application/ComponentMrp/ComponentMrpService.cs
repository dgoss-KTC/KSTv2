using Kst.Domain.ComponentMrp;
using Kst.Domain.LongTermShortages;

namespace Kst.Application.ComponentMrp;

public interface IComponentMrpReader
{
    Task<ComponentMrpSource?> ReadAsync(string site, string part, DateOnly horizonEnd, CancellationToken ct);
}

public sealed class DelegateComponentMrpReader(Func<string, string, DateOnly, CancellationToken, Task<ComponentMrpSource?>> read) : IComponentMrpReader
{
    public Task<ComponentMrpSource?> ReadAsync(string site, string part, DateOnly horizonEnd, CancellationToken ct) => read(site, part, horizonEnd, ct);
}

public sealed record ComponentMrpReport(string Site, DateOnly AsOfDate, int HorizonWeeks, bool IncludeUnconfirmed,
    ComponentMrpSource Source, LongTermShortageRow Projection)
{
    public string Part { get; init; } = Source.Input.ComponentPart;
    public required LongTermShortageRow ConfirmedProjection { get; init; }
    public required LongTermShortageRow AllReceiptsProjection { get; init; }
    public ComponentMrpReport Select(bool include) => this with
    {
        IncludeUnconfirmed = include,
        Projection = include ? AllReceiptsProjection : ConfirmedProjection
    };
}

public sealed class ComponentMrpService(IComponentMrpReader reader)
{
    public static bool ValidSite(string? site) => site is "SW" or "NW" or "AR" or "MN" or "MS" or "KV";

    public async Task<ComponentMrpReport?> GenerateAsync(string site, string part, int weeks, bool includeUnconfirmed,
        DateOnly asOf, CancellationToken ct)
    {
        if (!ValidSite(site) || string.IsNullOrWhiteSpace(part) || weeks is not (13 or 26 or 52 or 72))
            throw new ArgumentException("A supported site, part and horizon are required.");
        var source = await reader.ReadAsync(site, part, LongTermShortagesBuilder.GetWeekOneStart(asOf).AddDays(weeks * 7), ct);
        if (source is null) return null;
        ct.ThrowIfCancellationRequested();
        if (!string.Equals(source.Input.ComponentPart.Trim(), part.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("MRP source scope mismatch.");
        var pair = LongTermShortagesBuilder.BuildBoth(asOf, [source.Input], weeks);
        return new(site, asOf, weeks, includeUnconfirmed, source, (includeUnconfirmed ? pair.All : pair.Confirmed)[0])
        { Part = part, ConfirmedProjection = pair.Confirmed[0], AllReceiptsProjection = pair.All[0] };
    }
}

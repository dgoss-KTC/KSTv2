using Kst.Domain.LongTermShortages;

namespace Kst.Application.LongTermShortages;

public interface ILongTermShortageSourceReader
{
    Task<IReadOnlyList<LongTermShortageInput>> ReadAsync(
        string site,
        IReadOnlyDictionary<string, IReadOnlyList<string>> componentParents,
        DateOnly refreshDate,
        DateOnly horizonEnd,
        IReadOnlySet<string> workspaceParents,
        CancellationToken cancellationToken = default);
}

public sealed class DelegateLongTermShortageSourceReader(
    Func<string, IReadOnlyDictionary<string, IReadOnlyList<string>>, DateOnly, DateOnly, IReadOnlySet<string>, CancellationToken, Task<IReadOnlyList<LongTermShortageInput>>> read) : ILongTermShortageSourceReader
{
    public Task<IReadOnlyList<LongTermShortageInput>> ReadAsync(string site, IReadOnlyDictionary<string, IReadOnlyList<string>> componentParents, DateOnly refreshDate, DateOnly horizonEnd, IReadOnlySet<string> workspaceParents, CancellationToken cancellationToken = default) =>
        read(site, componentParents, refreshDate, horizonEnd, workspaceParents, cancellationToken);
}

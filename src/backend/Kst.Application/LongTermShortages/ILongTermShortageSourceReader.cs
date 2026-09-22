using Kst.Domain.LongTermShortages;
namespace Kst.Application.LongTermShortages;
public interface ILongTermShortageSourceReader
{
    Task<IReadOnlyList<LongTermShortageInput>> ReadAsync(string site, IReadOnlyDictionary<string, IReadOnlyList<string>> componentParents, DateOnly horizonEnd, CancellationToken cancellationToken = default);
}
public sealed class DelegateLongTermShortageSourceReader(Func<string, IReadOnlyDictionary<string, IReadOnlyList<string>>, DateOnly, CancellationToken, Task<IReadOnlyList<LongTermShortageInput>>> read) : ILongTermShortageSourceReader
{
    public Task<IReadOnlyList<LongTermShortageInput>> ReadAsync(string site, IReadOnlyDictionary<string, IReadOnlyList<string>> componentParents, DateOnly horizonEnd, CancellationToken cancellationToken = default) => read(site, componentParents, horizonEnd, cancellationToken);
}

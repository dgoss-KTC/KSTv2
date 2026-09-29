using Kst.Domain.LongTermShortages;
namespace Kst.Application.LongTermShortages;
public interface ILongTermShortageSourceReader
{
    Task<LongTermShortageAcquisition> ReadAsync(string site, IReadOnlyDictionary<string, IReadOnlyList<string>> componentParents, DateOnly refreshDate, DateOnly horizonEnd, CancellationToken cancellationToken = default);
}
public sealed class DelegateLongTermShortageSourceReader(Func<string, IReadOnlyDictionary<string, IReadOnlyList<string>>, DateOnly, DateOnly, CancellationToken, Task<LongTermShortageAcquisition>> read) : ILongTermShortageSourceReader
{
    public Task<LongTermShortageAcquisition> ReadAsync(string site, IReadOnlyDictionary<string, IReadOnlyList<string>> componentParents, DateOnly refreshDate, DateOnly horizonEnd, CancellationToken cancellationToken = default) => read(site, componentParents, refreshDate, horizonEnd, cancellationToken);
}

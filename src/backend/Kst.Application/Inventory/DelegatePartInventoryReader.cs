using Kst.Domain.Inventory;

namespace Kst.Application.Inventory;

/// <summary>
/// Generic delegate-backed <see cref="IPartInventoryReader"/>. Lets the composition root
/// (Kst.Api) adapt the concrete QAD adapter without Kst.Application referencing
/// Kst.Integrations.Qad.
/// </summary>
public sealed class DelegatePartInventoryReader : IPartInventoryReader
{
    private readonly Func<string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<PartInventorySummary>>> _read;

    public DelegatePartInventoryReader(
        Func<string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<PartInventorySummary>>> read)
    {
        _read = read;
    }

    public Task<IReadOnlyList<PartInventorySummary>> ReadSummariesAsync(
        string site,
        IReadOnlyList<string> partNumbers,
        CancellationToken cancellationToken = default) => _read(site, partNumbers, cancellationToken);
}

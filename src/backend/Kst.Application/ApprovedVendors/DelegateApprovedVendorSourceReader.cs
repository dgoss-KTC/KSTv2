using Kst.Domain.ApprovedVendors;

namespace Kst.Application.ApprovedVendors;

/// <summary>
/// Generic delegate-backed <see cref="IApprovedVendorSourceReader"/>. Lets the composition root
/// (Kst.Api) adapt the concrete QAD adapter without Kst.Application referencing
/// Kst.Integrations.Qad.
/// </summary>
public sealed class DelegateApprovedVendorSourceReader : IApprovedVendorSourceReader
{
    private readonly Func<string, string, CancellationToken, Task<IReadOnlyList<ApprovedVendor>>> _read;

    public DelegateApprovedVendorSourceReader(
        Func<string, string, CancellationToken, Task<IReadOnlyList<ApprovedVendor>>> read)
    {
        _read = read;
    }

    public Task<IReadOnlyList<ApprovedVendor>> ReadAsync(
        string site,
        string componentPart,
        CancellationToken cancellationToken = default) => _read(site, componentPart, cancellationToken);
}

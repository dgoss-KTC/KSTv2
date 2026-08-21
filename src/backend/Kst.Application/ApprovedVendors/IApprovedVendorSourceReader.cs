using Kst.Domain.ApprovedVendors;

namespace Kst.Application.ApprovedVendors;

/// <summary>
/// Reads Stage 8D.7 Approved Vendor List (AVL) relationships for one component part at one site.
/// Implementations live in Kst.Integrations.Qad; Kst.Api bridges the concrete adapter into this
/// interface via <see cref="DelegateApprovedVendorSourceReader"/> so Kst.Application never
/// references Kst.Integrations.Qad. AVL grain is Domain + Component Part (site is accepted only so
/// the QAD boundary can resolve Domain); returns zero-to-many rows, preserving source order and
/// multiplicity. A nonexistent component part naturally yields zero rows here — this reader does
/// not perform a separate part-existence check.
/// </summary>
public interface IApprovedVendorSourceReader
{
    Task<IReadOnlyList<ApprovedVendor>> ReadAsync(
        string site,
        string componentPart,
        CancellationToken cancellationToken = default);
}

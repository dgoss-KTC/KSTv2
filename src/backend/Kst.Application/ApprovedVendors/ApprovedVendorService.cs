using Kst.Application.Workspaces;
using Kst.Domain.ApprovedVendors;
using Microsoft.Extensions.Logging;

namespace Kst.Application.ApprovedVendors;

/// <summary>
/// Resolves lazily-loaded Stage 8D.7 Approved Vendors for a workspace-selected component part.
/// AVL business grain is Domain + Component Part, resolved at the QAD boundary from the
/// workspace's Site — the caller never supplies Domain. Deliberately has no cache and no MPS
/// snapshot dependency: AVL is reference/master information, not an MPS-derived snapshot, and the
/// request is cheap and explicitly lazy (the frontend only issues it on first AVL-section
/// expansion). A nonexistent component part is not distinguished from a zero-row AVL result — both
/// naturally produce <see cref="ApprovedVendorOutcomeKind.Loaded"/> with an empty collection,
/// since the modal only ever requests AVL after Component Detail has already established the
/// component exists. Cancellation from the reader propagates as-is — it is neither a load failure
/// nor a candidate for the Unavailable outcome.
/// </summary>
public sealed class ApprovedVendorService
{
    private readonly IWorkspaceConfigurationService _workspaces;
    private readonly IApprovedVendorSourceReader _sourceReader;
    private readonly ILogger<ApprovedVendorService> _logger;

    public ApprovedVendorService(
        IWorkspaceConfigurationService workspaces,
        IApprovedVendorSourceReader sourceReader,
        ILogger<ApprovedVendorService> logger)
    {
        _workspaces = workspaces;
        _sourceReader = sourceReader;
        _logger = logger;
    }

    public async Task<ApprovedVendorResult> GetApprovedVendorsAsync(
        Guid workspaceId,
        string componentPart,
        CancellationToken cancellationToken = default)
    {
        if (componentPart is null)
            throw new ArgumentNullException(nameof(componentPart));

        cancellationToken.ThrowIfCancellationRequested();
        var workspaces = await _workspaces.GetWorkspacesAsync();
        var workspace = workspaces.Workspaces.FirstOrDefault(w => w.AssignmentId == workspaceId)
            ?? throw new ApprovedVendorWorkspaceNotFoundException(workspaceId);

        var normalizedPart = componentPart.Trim();

        IReadOnlyList<ApprovedVendor> vendors;
        try
        {
            vendors = await _sourceReader.ReadAsync(workspace.Site, normalizedPart, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is control flow, not a load failure: it must never be converted into
            // an Unavailable outcome.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Approved Vendors load failed for workspace {WorkspaceId} component {ComponentPart}.",
                workspaceId, normalizedPart);

            return ApprovedVendorResult.Unavailable;
        }

        return ApprovedVendorResult.Loaded(vendors);
    }
}

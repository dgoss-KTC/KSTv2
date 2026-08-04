using Kst.Domain.Workspaces;
using Microsoft.Extensions.Logging;

namespace Kst.Application.Workspaces;

/// <summary>
/// Validates, normalizes, and persists workspace configuration.
/// </summary>
public sealed class WorkspaceConfigurationService : IWorkspaceConfigurationService
{
    private readonly IWorkspaceConfigurationStore _store;
    private readonly ILogger<WorkspaceConfigurationService> _logger;

    public WorkspaceConfigurationService(
        IWorkspaceConfigurationStore store,
        ILogger<WorkspaceConfigurationService> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task<WorkspaceListResult> GetWorkspacesAsync()
    {
        var result = await _store.LoadAsync();
        var ordered = result.Workspaces.OrderBy(w => w.SortOrder).ToList();
        return new WorkspaceListResult(ordered, result.ConfigurationWarning);
    }

    public async Task<WorkspaceCreateResult> CreateWorkspaceAsync(CreateWorkspaceCommand command)
    {
        var errors = Validate(command);
        if (errors.Count > 0)
            return WorkspaceCreateResult.Failure(errors);

        var site = command.Site!.Trim().ToUpperInvariant();
        var customerNumber = NormalizeOptionalString(command.CustomerNumber);
        var productLineFrom = NormalizeOptionalString(command.ProductLineFrom);
        var productLineTo = NormalizeOptionalString(command.ProductLineTo);

        // When ProductLineFrom is set and ProductLineTo is blank, treat as single product line.
        if (productLineFrom is not null && productLineTo is null)
            productLineTo = productLineFrom;

        var displayName = DeriveDisplayName(command.DisplayName, customerNumber, productLineFrom, productLineTo);

        var loaded = await _store.LoadAsync();
        var existing = loaded.Workspaces.ToList();

        if (IsDuplicateScope(existing, site, customerNumber, productLineFrom, productLineTo, excludeAssignmentId: null))
            return WorkspaceCreateResult.Failure([DuplicateScopeError]);

        var nextSortOrder = existing.Count == 0 ? 0 : existing.Max(w => w.SortOrder) + 1;

        var workspace = new WorkspaceAssignment(
            AssignmentId: Guid.NewGuid(),
            DisplayName: displayName,
            Site: site,
            CustomerNumber: customerNumber,
            ProductLineFrom: productLineFrom,
            ProductLineTo: productLineTo,
            IsTemporary: command.IsTemporary,
            CoverageEndsOn: command.CoverageEndsOn,
            IsEnabled: true,
            SortOrder: nextSortOrder
        );

        existing.Add(workspace);
        await _store.SaveAsync(existing);

        return WorkspaceCreateResult.Success(workspace);
    }

    public async Task<WorkspaceUpdateResult> UpdateWorkspaceAsync(Guid assignmentId, CreateWorkspaceCommand command)
    {
        var errors = Validate(command);
        if (errors.Count > 0)
            return WorkspaceUpdateResult.Failure(errors);

        var loaded = await _store.LoadAsync();
        var existing = loaded.Workspaces.ToList();
        var index = existing.FindIndex(w => w.AssignmentId == assignmentId);
        if (index < 0)
        {
            _logger.LogWarning("Update requested for unknown workspace assignment {AssignmentId}.", assignmentId);
            return WorkspaceUpdateResult.NotFoundResult();
        }

        var site = command.Site!.Trim().ToUpperInvariant();
        var customerNumber = NormalizeOptionalString(command.CustomerNumber);
        var productLineFrom = NormalizeOptionalString(command.ProductLineFrom);
        var productLineTo = NormalizeOptionalString(command.ProductLineTo);

        // When ProductLineFrom is set and ProductLineTo is blank, treat as single product line.
        if (productLineFrom is not null && productLineTo is null)
            productLineTo = productLineFrom;

        var displayName = DeriveDisplayName(command.DisplayName, customerNumber, productLineFrom, productLineTo);

        if (IsDuplicateScope(existing, site, customerNumber, productLineFrom, productLineTo, excludeAssignmentId: assignmentId))
            return WorkspaceUpdateResult.Failure([DuplicateScopeError]);

        var updated = existing[index] with
        {
            DisplayName = displayName,
            Site = site,
            CustomerNumber = customerNumber,
            ProductLineFrom = productLineFrom,
            ProductLineTo = productLineTo,
            IsTemporary = command.IsTemporary,
            CoverageEndsOn = command.CoverageEndsOn,
        };

        existing[index] = updated;
        await _store.SaveAsync(existing);

        return WorkspaceUpdateResult.Success(updated);
    }

    public Task<WorkspaceOperationResult> ArchiveWorkspaceAsync(Guid assignmentId) =>
        SetEnabledAsync(assignmentId, isEnabled: false);

    public Task<WorkspaceOperationResult> RestoreWorkspaceAsync(Guid assignmentId) =>
        SetEnabledAsync(assignmentId, isEnabled: true);

    public async Task<WorkspaceOperationResult> DeleteWorkspaceAsync(Guid assignmentId)
    {
        var loaded = await _store.LoadAsync();
        var existing = loaded.Workspaces.ToList();
        var index = existing.FindIndex(w => w.AssignmentId == assignmentId);
        if (index < 0)
        {
            _logger.LogWarning("Delete requested for unknown workspace assignment {AssignmentId}.", assignmentId);
            return WorkspaceOperationResult.NotFoundResult();
        }

        existing.RemoveAt(index);
        await _store.SaveAsync(existing);

        return WorkspaceOperationResult.Success();
    }

    public async Task ResetWorkspacesAsync()
    {
        _logger.LogInformation("Resetting all workspace configuration.");
        await _store.SaveAsync([]);
    }

    public async Task<WorkspaceReorderResult> ReorderWorkspacesAsync(ReorderWorkspacesCommand command)
    {
        var ids = command.AssignmentIds ?? [];
        var errors = new List<WorkspaceValidationError>();

        if (ids.Count != ids.Distinct().Count())
            errors.Add(new WorkspaceValidationError("assignmentIds", "Duplicate assignment ids are not allowed."));

        var loaded = await _store.LoadAsync();
        var existing = loaded.Workspaces.ToList();
        var enabledIds = existing.Where(w => w.IsEnabled).Select(w => w.AssignmentId).ToHashSet();

        if (errors.Count == 0 && !ids.ToHashSet().SetEquals(enabledIds))
        {
            errors.Add(new WorkspaceValidationError(
                "assignmentIds",
                "The provided assignment ids must exactly match the currently active workspaces."));
        }

        if (errors.Count > 0)
            return WorkspaceReorderResult.Failure(errors);

        var byId = existing.ToDictionary(w => w.AssignmentId);
        var reordered = new List<WorkspaceAssignment>(existing.Count);
        for (var i = 0; i < ids.Count; i++)
            reordered.Add(byId[ids[i]] with { SortOrder = i });

        var archived = existing.Where(w => !w.IsEnabled).OrderBy(w => w.SortOrder).ToList();
        var nextOrder = reordered.Count;
        foreach (var ws in archived)
        {
            reordered.Add(ws with { SortOrder = nextOrder });
            nextOrder++;
        }

        await _store.SaveAsync(reordered);

        var ordered = reordered.OrderBy(w => w.SortOrder).ToList();
        return WorkspaceReorderResult.Success(ordered);
    }

    private async Task<WorkspaceOperationResult> SetEnabledAsync(Guid assignmentId, bool isEnabled)
    {
        var loaded = await _store.LoadAsync();
        var existing = loaded.Workspaces.ToList();
        var index = existing.FindIndex(w => w.AssignmentId == assignmentId);
        if (index < 0)
        {
            _logger.LogWarning(
                "{Operation} requested for unknown workspace assignment {AssignmentId}.",
                isEnabled ? "Restore" : "Archive", assignmentId);
            return WorkspaceOperationResult.NotFoundResult();
        }

        var updated = existing[index] with { IsEnabled = isEnabled };
        existing[index] = updated;
        await _store.SaveAsync(existing);

        return WorkspaceOperationResult.Success(updated);
    }

    private static readonly WorkspaceValidationError DuplicateScopeError = new(
        "scope",
        "A workspace with this site, customer number, and product-line range already exists.");

    /// <summary>
    /// Structural duplicate protection: the same site + customer number + product-line range must
    /// not exist among currently enabled (active) workspaces. Archived assignments are excluded so
    /// they never block creating a legitimate new active assignment. Friendly names may duplicate.
    /// </summary>
    private static bool IsDuplicateScope(
        IReadOnlyList<WorkspaceAssignment> existing,
        string site,
        string? customerNumber,
        string? productLineFrom,
        string? productLineTo,
        Guid? excludeAssignmentId) =>
        existing.Any(w =>
            w.IsEnabled &&
            w.AssignmentId != excludeAssignmentId &&
            string.Equals(w.Site, site, StringComparison.Ordinal) &&
            string.Equals(w.CustomerNumber, customerNumber, StringComparison.Ordinal) &&
            string.Equals(w.ProductLineFrom, productLineFrom, StringComparison.Ordinal) &&
            string.Equals(w.ProductLineTo, productLineTo, StringComparison.Ordinal));

    private static IReadOnlyList<WorkspaceValidationError> Validate(CreateWorkspaceCommand cmd)
    {
        var errors = new List<WorkspaceValidationError>();

        var site = cmd.Site?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(site))
            errors.Add(new WorkspaceValidationError("site", "Site is required."));
        else if (site.Length != 2)
            errors.Add(new WorkspaceValidationError("site", "Site must be exactly 2 characters."));
        else if (!site.All(char.IsLetter))
            errors.Add(new WorkspaceValidationError("site", "Site must contain letters only."));

        var customerNumber = cmd.CustomerNumber?.Trim();
        if (!string.IsNullOrEmpty(customerNumber))
        {
            if (customerNumber.Length != 8 || !customerNumber.All(char.IsDigit))
                errors.Add(new WorkspaceValidationError("customerNumber", "Customer number must be exactly 8 digits."));
        }

        var productLineFrom = cmd.ProductLineFrom?.Trim();
        if (!string.IsNullOrEmpty(productLineFrom))
        {
            if (productLineFrom.Length != 4 || !productLineFrom.All(char.IsDigit))
                errors.Add(new WorkspaceValidationError("productLineFrom", "Product Line From must be exactly 4 digits."));
        }

        var productLineTo = cmd.ProductLineTo?.Trim();
        if (!string.IsNullOrEmpty(productLineTo))
        {
            if (string.IsNullOrEmpty(productLineFrom))
                errors.Add(new WorkspaceValidationError("productLineTo", "Product Line To requires Product Line From."));
            else if (productLineTo.Length != 4 || !productLineTo.All(char.IsDigit))
                errors.Add(new WorkspaceValidationError("productLineTo", "Product Line To must be exactly 4 digits."));
            else if (string.Compare(productLineTo, productLineFrom, StringComparison.Ordinal) < 0)
                errors.Add(new WorkspaceValidationError("productLineTo", "Product Line To must be greater than or equal to Product Line From."));
        }

        // Check scope requirement only when basic field validation has passed
        if (errors.Count == 0)
        {
            var hasCustomer = !string.IsNullOrEmpty(customerNumber);
            var hasProductLine = !string.IsNullOrEmpty(productLineFrom);
            if (!hasCustomer && !hasProductLine)
                errors.Add(new WorkspaceValidationError("scope", "A workspace requires a customer number, a product-line range, or both."));
        }

        return errors;
    }

    private static string? NormalizeOptionalString(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string DeriveDisplayName(
        string? displayName,
        string? customerNumber,
        string? productLineFrom,
        string? productLineTo)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.Trim();

        if (customerNumber is not null && productLineFrom is not null)
        {
            var plLabel = productLineTo != productLineFrom
                ? $"PL {productLineFrom}\u2013{productLineTo}"
                : $"PL {productLineFrom}";
            return $"Customer {customerNumber} \u00b7 {plLabel}";
        }

        if (customerNumber is not null)
            return $"Customer {customerNumber}";

        if (productLineFrom is not null)
        {
            return productLineTo != productLineFrom
                ? $"PL {productLineFrom}\u2013{productLineTo}"
                : $"PL {productLineFrom}";
        }

        return "Workspace";
    }
}

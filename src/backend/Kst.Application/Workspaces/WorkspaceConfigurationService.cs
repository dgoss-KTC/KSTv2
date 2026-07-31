using Kst.Domain.Workspaces;

namespace Kst.Application.Workspaces;

/// <summary>
/// Validates, normalizes, and persists workspace configuration.
/// </summary>
public sealed class WorkspaceConfigurationService : IWorkspaceConfigurationService
{
    private readonly IWorkspaceConfigurationStore _store;

    public WorkspaceConfigurationService(IWorkspaceConfigurationStore store)
    {
        _store = store;
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

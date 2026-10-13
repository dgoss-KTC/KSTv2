namespace Kst.Domain.HistoricalShipments;

/// <summary>Independent workspace listing; dates apply to Ship Date, never Invoice Date.</summary>
public sealed record WorkspaceShipmentsFilter(
    string Site,
    IReadOnlyList<string> ParentParts,
    string? PartNumber,
    DateOnly? ShipDateFrom,
    DateOnly? ShipDateTo)
{
    public WorkspaceShipmentsFilter Validate()
    {
        var site = Site?.Trim().ToUpperInvariant();
        if (site is null || !HistoricalShipmentsRules.Sites.Contains(site)) throw new ArgumentException("Select one supported site.");
        if (ParentParts is null) throw new ArgumentException("Workspace parent scope is required.");
        var part = ValidateRequest(PartNumber, ShipDateFrom, ShipDateTo);
        return this with { Site = site, ParentParts = Array.AsReadOnly(ParentParts.ToArray()), PartNumber = part };
    }

    /// <summary>Validates caller-owned criteria before workspace lookup or QAD scope resolution.</summary>
    public static string? ValidateRequest(string? partNumber, DateOnly? shipDateFrom, DateOnly? shipDateTo)
    {
        var part = string.IsNullOrWhiteSpace(partNumber) ? null : partNumber.Trim();
        if (part?.Length > 30 || part?.Any(char.IsControl) == true) throw new ArgumentException("Invalid Part Number length or control character.");
        if (shipDateFrom > shipDateTo) throw new ArgumentException("Ship Date From must not exceed Ship Date To.");
        return part;
    }

    public HistoricalShipmentsFilter Projection() => new(Site, null, null, null, null, null, null, null, null, null,
        null, null, HistoricalShipmentsRules.DefaultColumnIds);
}

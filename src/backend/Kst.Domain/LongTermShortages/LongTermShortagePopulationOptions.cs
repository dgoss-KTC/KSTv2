namespace Kst.Domain.LongTermShortages;

/// <summary>
/// Stage 11 component report controls. They select which
/// BOM-selected components enter the site-wide projection; they are not client-side row-display
/// filters. Purchased components are included by default; manufactured components require
/// <see cref="IncludeManufacturedParts"/>. Phantoms are traversed but never displayed.
/// </summary>
public sealed record LongTermShortagePopulationOptions(
    bool IncludeManufacturedParts = false,
    bool IncludePhantoms = false,
    bool IncludeUnconfirmed = false,
    int HorizonWeeks = 26,
    bool ShowAll = false)
{
    public static readonly LongTermShortagePopulationOptions Default = new();

    /// <summary>Retained for existing consumers; the Stage 11 service applies effective P/M and hides phantoms.</summary>
    public bool IsIncluded(bool isManufactured, bool isPhantom) =>
        (!isManufactured || IncludeManufacturedParts) && (!isPhantom || IncludePhantoms);
}

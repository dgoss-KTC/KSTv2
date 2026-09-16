namespace Kst.Domain.LongTermShortages;

/// <summary>
/// Stage 11-A report-population options (owner-accepted defaults: both off). They select which
/// BOM-selected components enter the site-wide projection; they are not client-side row-display
/// filters. A normal component is included by default; a manufactured component requires
/// <see cref="IncludeManufacturedParts"/>; a phantom component requires <see cref="IncludePhantoms"/>;
/// a component that is both manufactured and phantom requires both options.
/// </summary>
public sealed record LongTermShortagePopulationOptions(
    bool IncludeManufacturedParts = false,
    bool IncludePhantoms = false)
{
    public static readonly LongTermShortagePopulationOptions Default = new();

    /// <summary>Applies the accepted population rule to one component's established part-master classifications.</summary>
    public bool IsIncluded(bool isManufactured, bool isPhantom) =>
        (!isManufactured || IncludeManufacturedParts) && (!isPhantom || IncludePhantoms);
}

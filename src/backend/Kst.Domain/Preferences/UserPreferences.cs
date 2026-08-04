namespace Kst.Domain.Preferences;

/// <summary>
/// Color theme preference. System follows the OS/browser media preference.
/// </summary>
public enum ThemePreference
{
    System,
    Light,
    Dark
}

/// <summary>
/// Bounded accent-color palette. Arbitrary hex input is not supported.
/// </summary>
public enum AccentColorPreference
{
    Blue,
    Teal,
    Amber
}

/// <summary>
/// Row density used by shell lists and future data grids.
/// </summary>
public enum RowDensityPreference
{
    Compact,
    Comfortable
}

/// <summary>
/// Application-owned, locally persisted user preferences. Stored separately from workspace assignments.
/// </summary>
public sealed record UserPreferences(
    ThemePreference Theme,
    AccentColorPreference AccentColor,
    RowDensityPreference RowDensity
)
{
    public static readonly UserPreferences Default = new(
        ThemePreference.System,
        AccentColorPreference.Blue,
        RowDensityPreference.Compact
    );
}

namespace Kst.Application.Preferences;

/// <summary>
/// Raw, unvalidated preference update request. Field values follow the same casing as the
/// domain enum names (case-insensitive), e.g. "system"/"light"/"dark".
/// </summary>
public sealed record UpdatePreferencesCommand(
    string? Theme,
    string? AccentColor,
    string? RowDensity
);

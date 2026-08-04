using Kst.Domain.Preferences;

namespace Kst.Application.Preferences;

public sealed record PreferencesLoadResult(
    UserPreferences Preferences,
    string? ConfigurationWarning
);

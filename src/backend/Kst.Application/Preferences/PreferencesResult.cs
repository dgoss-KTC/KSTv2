using Kst.Domain.Preferences;

namespace Kst.Application.Preferences;

public sealed record PreferencesResult(
    UserPreferences Preferences,
    string? ConfigurationWarning
);

public sealed record PreferencesUpdateResult
{
    public UserPreferences? Preferences { get; init; }
    public IReadOnlyList<PreferenceValidationError>? ValidationErrors { get; init; }

    public bool IsSuccess => Preferences is not null;

    public static PreferencesUpdateResult Success(UserPreferences preferences) =>
        new() { Preferences = preferences };

    public static PreferencesUpdateResult Failure(IReadOnlyList<PreferenceValidationError> errors) =>
        new() { ValidationErrors = errors };
}

using Kst.Domain.Preferences;
using Microsoft.Extensions.Logging;

namespace Kst.Application.Preferences;

/// <summary>
/// Validates, normalizes, and persists user preferences.
/// </summary>
public sealed class PreferencesService : IPreferencesService
{
    private readonly IPreferencesStore _store;
    private readonly ILogger<PreferencesService> _logger;

    public PreferencesService(IPreferencesStore store, ILogger<PreferencesService> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task<PreferencesResult> GetPreferencesAsync()
    {
        var result = await _store.LoadAsync();
        return new PreferencesResult(result.Preferences, result.ConfigurationWarning);
    }

    public async Task<PreferencesUpdateResult> UpdatePreferencesAsync(UpdatePreferencesCommand command)
    {
        var errors = new List<PreferenceValidationError>();

        var theme = ParseEnum<ThemePreference>(command.Theme, "theme", errors);
        var accentColor = ParseEnum<AccentColorPreference>(command.AccentColor, "accentColor", errors);
        var rowDensity = ParseEnum<RowDensityPreference>(command.RowDensity, "rowDensity", errors);

        if (errors.Count > 0)
            return PreferencesUpdateResult.Failure(errors);

        var preferences = new UserPreferences(theme!.Value, accentColor!.Value, rowDensity!.Value);
        await _store.SaveAsync(preferences);

        _logger.LogInformation(
            "Preferences updated. Theme={Theme} AccentColor={AccentColor} RowDensity={RowDensity}",
            preferences.Theme, preferences.AccentColor, preferences.RowDensity);

        return PreferencesUpdateResult.Success(preferences);
    }

    private static TEnum? ParseEnum<TEnum>(string? value, string field, List<PreferenceValidationError> errors)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new PreferenceValidationError(field, $"{field} is required."));
            return null;
        }

        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            var allowed = string.Join(", ", Enum.GetNames<TEnum>());
            errors.Add(new PreferenceValidationError(field, $"{field} must be one of: {allowed}."));
            return null;
        }

        return parsed;
    }
}

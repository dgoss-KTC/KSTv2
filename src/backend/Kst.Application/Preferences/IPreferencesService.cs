namespace Kst.Application.Preferences;

/// <summary>
/// Application service for reading and updating user preferences.
/// </summary>
public interface IPreferencesService
{
    Task<PreferencesResult> GetPreferencesAsync();
    Task<PreferencesUpdateResult> UpdatePreferencesAsync(UpdatePreferencesCommand command);
}

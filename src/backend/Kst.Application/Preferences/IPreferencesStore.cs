using Kst.Domain.Preferences;

namespace Kst.Application.Preferences;

/// <summary>
/// Persistence contract for user preferences. Implemented in Kst.Infrastructure.
/// </summary>
public interface IPreferencesStore
{
    Task<PreferencesLoadResult> LoadAsync();
    Task SaveAsync(UserPreferences preferences);
}

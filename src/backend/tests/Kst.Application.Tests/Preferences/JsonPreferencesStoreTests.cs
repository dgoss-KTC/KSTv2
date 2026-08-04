using System.Text.Json;
using Kst.Application.Preferences;
using Kst.Domain.Preferences;
using Kst.Infrastructure.Configuration;
using Kst.Infrastructure.Preferences;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.Preferences;

public sealed class JsonPreferencesStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalAppDataPaths _paths;
    private readonly JsonPreferencesStore _store;

    public JsonPreferencesStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kst-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _paths = new LocalAppDataPaths(_tempDir);
        _store = new JsonPreferencesStore(_paths, NullLogger<JsonPreferencesStore>.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task Load_Returns_Defaults_When_File_Missing()
    {
        var result = await _store.LoadAsync();

        Assert.Equal(UserPreferences.Default, result.Preferences);
        Assert.Null(result.ConfigurationWarning);
    }

    [Fact]
    public async Task Save_And_Reload_Persists_Preferences()
    {
        var preferences = new UserPreferences(
            ThemePreference.Dark, AccentColorPreference.Teal, RowDensityPreference.Comfortable);

        await _store.SaveAsync(preferences);
        var result = await _store.LoadAsync();

        Assert.Equal(preferences, result.Preferences);
        Assert.Null(result.ConfigurationWarning);
    }

    [Fact]
    public async Task Save_Writes_CamelCase_Json()
    {
        var preferences = new UserPreferences(
            ThemePreference.Light, AccentColorPreference.Amber, RowDensityPreference.Compact);

        await _store.SaveAsync(preferences);

        var json = await File.ReadAllTextAsync(_paths.PreferencesFilePath);
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("theme", out var theme));
        Assert.Equal("light", theme.GetString());
        Assert.True(doc.RootElement.TryGetProperty("accentColor", out var accent));
        Assert.Equal("amber", accent.GetString());
        Assert.True(doc.RootElement.TryGetProperty("rowDensity", out var density));
        Assert.Equal("compact", density.GetString());
    }

    [Fact]
    public async Task Corrupt_File_Returns_Defaults_With_Warning()
    {
        _paths.EnsureDirectoriesExist();
        await File.WriteAllTextAsync(_paths.PreferencesFilePath, "{ NOT VALID JSON !!!");

        var result = await _store.LoadAsync();

        Assert.Equal(UserPreferences.Default, result.Preferences);
        Assert.NotNull(result.ConfigurationWarning);
        Assert.False(File.Exists(_paths.PreferencesFilePath));
    }

    [Fact]
    public async Task Corrupt_File_Is_Renamed_With_Invalid_Suffix()
    {
        _paths.EnsureDirectoriesExist();
        await File.WriteAllTextAsync(_paths.PreferencesFilePath, "NOT JSON");

        await _store.LoadAsync();

        var invalidFiles = Directory.GetFiles(_paths.ConfigDirectory, "preferences.*.invalid.json");
        Assert.Single(invalidFiles);
    }

    [Fact]
    public async Task Save_Is_Idempotent_For_Multiple_Calls()
    {
        await _store.SaveAsync(new UserPreferences(
            ThemePreference.Dark, AccentColorPreference.Blue, RowDensityPreference.Compact));
        await _store.SaveAsync(new UserPreferences(
            ThemePreference.Light, AccentColorPreference.Teal, RowDensityPreference.Comfortable));

        var result = await _store.LoadAsync();
        Assert.Equal(ThemePreference.Light, result.Preferences.Theme);
        Assert.Equal(AccentColorPreference.Teal, result.Preferences.AccentColor);
        Assert.Equal(RowDensityPreference.Comfortable, result.Preferences.RowDensity);
    }

    // --- Service-level validation and persistence-across-restart behavior ---

    private PreferencesService BuildService() =>
        new(_store, NullLogger<PreferencesService>.Instance);

    private PreferencesService BuildReloadedService()
    {
        var reloadedStore = new JsonPreferencesStore(_paths, NullLogger<JsonPreferencesStore>.Instance);
        return new PreferencesService(reloadedStore, NullLogger<PreferencesService>.Instance);
    }

    [Fact]
    public async Task UpdatePreferences_Persists_Across_Reload()
    {
        var service = BuildService();
        var result = await service.UpdatePreferencesAsync(new UpdatePreferencesCommand("dark", "teal", "comfortable"));
        Assert.True(result.IsSuccess);

        var reloaded = await BuildReloadedService().GetPreferencesAsync();

        Assert.Equal(ThemePreference.Dark, reloaded.Preferences.Theme);
        Assert.Equal(AccentColorPreference.Teal, reloaded.Preferences.AccentColor);
        Assert.Equal(RowDensityPreference.Comfortable, reloaded.Preferences.RowDensity);
    }

    [Theory]
    [InlineData(null, "blue", "compact", "theme")]
    [InlineData("dark", "purple", "compact", "accentColor")]
    [InlineData("dark", "blue", "roomy", "rowDensity")]
    public async Task UpdatePreferences_Rejects_Invalid_Values(
        string? theme, string accentColor, string rowDensity, string expectedField)
    {
        var service = BuildService();

        var result = await service.UpdatePreferencesAsync(
            new UpdatePreferencesCommand(theme, accentColor, rowDensity));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.ValidationErrors!, e => e.Field == expectedField);
    }

    [Fact]
    public async Task UpdatePreferences_Is_CaseInsensitive()
    {
        var service = BuildService();

        var result = await service.UpdatePreferencesAsync(new UpdatePreferencesCommand("DARK", "TEAL", "COMPACT"));

        Assert.True(result.IsSuccess);
        Assert.Equal(ThemePreference.Dark, result.Preferences!.Theme);
        Assert.Equal(AccentColorPreference.Teal, result.Preferences!.AccentColor);
        Assert.Equal(RowDensityPreference.Compact, result.Preferences!.RowDensity);
    }
}

namespace Kst.Api.Dtos;

public sealed record UserPreferencesDto(
    string Theme,
    string AccentColor,
    string RowDensity
);

public sealed record PreferencesResponseDto(
    UserPreferencesDto Preferences,
    string? ConfigurationWarning
);

public sealed record UpdatePreferencesRequestDto(
    string Theme,
    string AccentColor,
    string RowDensity
);

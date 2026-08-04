using Kst.Api.Dtos;
using Kst.Application.Preferences;
using Kst.Domain.Preferences;

namespace Kst.Api.Endpoints;

/// <summary>
/// User preferences endpoints (theme, accent color, row density). Preferences are local-only and
/// independent from workspace configuration.
/// </summary>
public static class PreferencesEndpoints
{
    public static void MapPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/preferences", GetPreferences)
            .WithName("GetPreferences")
            .WithSummary("Returns the current user preferences and any nonfatal configuration warning.")
            .WithTags("Preferences")
            .Produces<PreferencesResponseDto>();

        app.MapPut("/api/v1/preferences", UpdatePreferences)
            .WithName("UpdatePreferences")
            .WithSummary("Validates and persists updated user preferences.")
            .WithTags("Preferences")
            .Produces<PreferencesResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetPreferences(IPreferencesService service)
    {
        var result = await service.GetPreferencesAsync();
        return Results.Ok(new PreferencesResponseDto(ToDto(result.Preferences), result.ConfigurationWarning));
    }

    private static async Task<IResult> UpdatePreferences(
        UpdatePreferencesRequestDto request,
        IPreferencesService service)
    {
        var command = new UpdatePreferencesCommand(request.Theme, request.AccentColor, request.RowDensity);
        var result = await service.UpdatePreferencesAsync(command);

        if (!result.IsSuccess)
        {
            return Results.ValidationProblem(
                result.ValidationErrors!
                    .GroupBy(e => e.Field)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.Message).ToArray()));
        }

        return Results.Ok(new PreferencesResponseDto(ToDto(result.Preferences!), ConfigurationWarning: null));
    }

    private static UserPreferencesDto ToDto(UserPreferences preferences) =>
        new(
            Theme: preferences.Theme.ToString().ToLowerInvariant(),
            AccentColor: preferences.AccentColor.ToString().ToLowerInvariant(),
            RowDensity: preferences.RowDensity.ToString().ToLowerInvariant());
}

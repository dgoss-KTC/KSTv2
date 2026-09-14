using System.Text.Json;

namespace Kst.Integrations.Shortages.Options;

/// <summary>
/// Loads Shortages SQL-authentication settings from the Tauri-supplied bundled-resource path.
/// Invalid configuration deliberately has no diagnostic detail so secret values cannot escape.
/// </summary>
public sealed class ShortagesSecretFileLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string? _filePath;

    public ShortagesSecretFileLoader(string? filePath = null)
    {
        _filePath = filePath ?? ShortagesSecretFilePath.Resolve();
    }

    public bool TryLoad(out ShortagesSqlConnectionSettings? settings)
    {
        settings = null;

        try
        {
            if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath))
                return false;

            var document = JsonSerializer.Deserialize<ShortagesSecretDocument>(File.ReadAllText(_filePath), JsonOptions);
            var candidate = document?.Shortages;
            if (candidate is null || !candidate.HasRequiredFields())
                return false;

            settings = candidate;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public sealed record ShortagesSqlConnectionSettings
    {
        public string? Server { get; init; }
        public string? Database { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }
        public bool Encrypt { get; init; } = true;
        public bool TrustServerCertificate { get; init; }

        internal bool HasRequiredFields() =>
            !string.IsNullOrWhiteSpace(Server)
            && !string.IsNullOrWhiteSpace(Database)
            && !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password);
    }

    private sealed record ShortagesSecretDocument(ShortagesSqlConnectionSettings? Shortages);
}

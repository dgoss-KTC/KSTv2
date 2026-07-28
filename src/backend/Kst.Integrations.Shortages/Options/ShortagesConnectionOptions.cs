namespace Kst.Integrations.Shortages.Options;

/// <summary>
/// Configuration options for the internal shortage database connection.
/// Uses Windows-integrated authentication; no password fields.
/// </summary>
public sealed record ShortagesConnectionOptions
{
    public const string SectionName = "ShortagesDatabase";

    public string? Server { get; init; }
    public string? Database { get; init; }
    public int ConnectTimeoutSeconds { get; init; } = 30;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Server)
                             && !string.IsNullOrWhiteSpace(Database);
}

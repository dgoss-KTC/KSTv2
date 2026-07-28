namespace Kst.Integrations.Qad.Options;

/// <summary>
/// Configuration options for the QAD ERP database connection.
/// Uses Windows-integrated authentication; no password fields.
/// </summary>
public sealed record QadConnectionOptions
{
    public const string SectionName = "QadDatabase";

    /// <summary>
    /// SQL Server hostname or instance name. Null when not yet configured.
    /// </summary>
    public string? Server { get; init; }

    /// <summary>
    /// QAD database name on the SQL Server instance.
    /// </summary>
    public string? Database { get; init; }

    /// <summary>
    /// Connection timeout in seconds. Defaults to 30.
    /// </summary>
    public int ConnectTimeoutSeconds { get; init; } = 30;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Server)
                             && !string.IsNullOrWhiteSpace(Database);
}

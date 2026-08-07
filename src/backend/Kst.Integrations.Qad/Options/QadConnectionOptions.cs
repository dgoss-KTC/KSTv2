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

    /// <summary>
    /// Command timeout in seconds for MPS source reads. Backend-configurable; not an end-user setting.
    /// Accepted Stage 5A default is 60 seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Encrypts the connection. Defaults to true; combined with <see cref="TrustServerCertificate"/>
    /// so on-prem SQL Server 2016 instances without a CA-signed certificate remain reachable.
    /// </summary>
    public bool Encrypt { get; init; } = true;

    /// <summary>
    /// Trusts the server certificate without validating it against a CA. Only meaningful when
    /// <see cref="Encrypt"/> is true; required for typical on-prem SQL Server 2016 deployments.
    /// </summary>
    public bool TrustServerCertificate { get; init; } = true;

    /// <summary>
    /// Maximum resolved parent-part parameters per MPS query batch. Not an end-user setting.
    /// </summary>
    public int MaxPartBatchSize { get; init; } = 500;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Server)
                             && !string.IsNullOrWhiteSpace(Database);
}


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
    /// Encrypts the connection. Defaults to false because the verified legacy QAD SQL endpoint does
    /// not support encrypted client connections from existing supported clients. This is a legacy
    /// infrastructure constraint, not the desired future state: when the QAD SQL infrastructure
    /// later supports TLS, the target is Encrypt=true / TrustServerCertificate=false. See
    /// docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md.
    /// </summary>
    public bool Encrypt { get; init; } = false;

    /// <summary>
    /// Trusts the server certificate without validating it against a CA. Defaults to false. With
    /// the current Encrypt=false legacy transport, certificate trust is not applicable and must not
    /// be used as a substitute for disabling encryption; it also stays false for the future
    /// Encrypt=true target state.
    /// </summary>
    public bool TrustServerCertificate { get; init; } = false;

    /// <summary>
    /// Maximum resolved parent-part parameters per MPS query batch. Not an end-user setting.
    /// </summary>
    public int MaxPartBatchSize { get; init; } = 500;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Server)
                             && !string.IsNullOrWhiteSpace(Database);
}


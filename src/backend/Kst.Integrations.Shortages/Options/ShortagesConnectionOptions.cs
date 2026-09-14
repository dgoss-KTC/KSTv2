namespace Kst.Integrations.Shortages.Options;

/// <summary>
/// Non-secret runtime state for the internal shortage database connection.
/// SQL-authenticated endpoint and credential values are loaded only from the external secret file.
/// </summary>
public sealed record ShortagesConnectionOptions(bool IsConfigured = false)
{
    public const string SectionName = "ShortagesDatabase";
}

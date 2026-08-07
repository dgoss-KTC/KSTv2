using Microsoft.Data.SqlClient;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad;

/// <summary>
/// Builds the QAD SQL Server connection string from <see cref="QadConnectionOptions"/>.
/// Always Windows-integrated authentication; never accepts or logs credentials.
/// </summary>
public static class QadConnectionStringFactory
{
    public static string Build(QadConnectionOptions options)
    {
        if (!options.IsConfigured)
            throw new InvalidOperationException("QAD connection is not configured.");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = options.Server,
            InitialCatalog = options.Database,
            IntegratedSecurity = true,
            ConnectTimeout = options.ConnectTimeoutSeconds,
            Encrypt = options.Encrypt,
            TrustServerCertificate = options.TrustServerCertificate,
            ApplicationName = "KST v2"
        };

        return builder.ConnectionString;
    }
}

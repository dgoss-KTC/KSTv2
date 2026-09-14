using Kst.Integrations.Shortages.Options;
using Microsoft.Data.SqlClient;

namespace Kst.Integrations.Shortages;

/// <summary>
/// Builds a Shortages SQL-authenticated connection string without opening a database connection.
/// </summary>
public static class ShortagesConnectionStringFactory
{
    public static string Build(ShortagesSecretFileLoader.ShortagesSqlConnectionSettings settings)
    {
        if (!settings.HasRequiredFields())
            throw new InvalidOperationException("Shortages connection is not configured.");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = settings.Server,
            InitialCatalog = settings.Database,
            UserID = settings.Username,
            Password = settings.Password,
            IntegratedSecurity = false,
            Encrypt = settings.Encrypt,
            TrustServerCertificate = settings.TrustServerCertificate,
            ApplicationName = "KST v2"
        };

        return builder.ConnectionString;
    }
}

using Microsoft.Data.SqlClient;
using Kst.Integrations.Qad;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Tests.Connection;

/// <summary>
/// Regression coverage for S0.2-F003 (QAD SQL transport configuration mismatch), remediated in S0.4A.
///
/// The effective QAD connection string is produced by <see cref="QadConnectionStringFactory"/> from
/// <see cref="QadConnectionOptions"/>. These tests assert the *effective, generated* connection string
/// — parsed back through <see cref="SqlConnectionStringBuilder"/> without opening any connection — so
/// the confirmed mismatch (Encrypt=true / TrustServerCertificate=true) cannot silently return, whether
/// it is reintroduced through the options defaults or the factory.
///
/// The verified legacy QAD endpoint does not support encrypted client connections, so the effective QAD
/// transport must be Encrypt=false (explicit, not a client-library default) with TrustServerCertificate
/// not set to true, Windows Integrated Authentication, and no SQL username/password. The future target
/// (when QAD SQL supports TLS) is Encrypt=true / TrustServerCertificate=false. See
/// docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md.
/// </summary>
public sealed class QadConnectionStringFactoryTests
{
    // Mirrors the checked-in appsettings.json "QadDatabase" section, which supplies Server/Database but
    // no transport overrides — so Encrypt/TrustServerCertificate resolve to the options defaults.
    private static QadConnectionOptions DefaultConfiguredOptions() =>
        new() { Server = "TESTSERVER", Database = "TESTDB" };

    [Fact]
    public void Effective_Qad_ConnectionString_Explicitly_Disables_Encryption()
    {
        var connectionString = QadConnectionStringFactory.Build(DefaultConfiguredOptions());
        var builder = new SqlConnectionStringBuilder(connectionString);

        // "Encrypt=false" is represented as SqlConnectionEncryptOption.Optional in
        // Microsoft.Data.SqlClient (bool false -> Optional; bool true -> Mandatory). This must be
        // explicit, not left to the client-library default (which enforces encryption).
        Assert.Equal(SqlConnectionEncryptOption.Optional, builder.Encrypt);
    }

    [Fact]
    public void Effective_Qad_ConnectionString_Uses_Windows_Integrated_Authentication()
    {
        var connectionString = QadConnectionStringFactory.Build(DefaultConfiguredOptions());
        var builder = new SqlConnectionStringBuilder(connectionString);

        Assert.True(builder.IntegratedSecurity);
    }

    [Fact]
    public void Effective_Qad_ConnectionString_Does_Not_Trust_Server_Certificate()
    {
        var connectionString = QadConnectionStringFactory.Build(DefaultConfiguredOptions());
        var builder = new SqlConnectionStringBuilder(connectionString);

        // TrustServerCertificate=true must be absent from the effective QAD configuration.
        Assert.False(builder.TrustServerCertificate);
    }

    [Fact]
    public void Effective_Qad_ConnectionString_Carries_No_Sql_User_Id_Or_Password()
    {
        var connectionString = QadConnectionStringFactory.Build(DefaultConfiguredOptions());
        var builder = new SqlConnectionStringBuilder(connectionString);

        Assert.True(string.IsNullOrEmpty(builder.UserID));
        Assert.True(builder.Password is null || builder.Password.Length == 0);
    }
}

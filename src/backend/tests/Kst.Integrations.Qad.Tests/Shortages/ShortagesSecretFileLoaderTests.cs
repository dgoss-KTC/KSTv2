using Kst.Integrations.Shortages;
using Kst.Integrations.Shortages.Options;
using Microsoft.Data.SqlClient;

namespace Kst.Integrations.Qad.Tests.Shortages;

public sealed class ShortagesSecretFileLoaderTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"kst-shortages-test-{Guid.NewGuid():N}");

    public ShortagesSecretFileLoaderTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        try { Directory.Delete(_tempDirectory, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Resolve_Returns_Null_When_Tauri_Does_Not_Supply_A_Path()
    {
        var path = ShortagesSecretFilePath.Resolve(null);

        Assert.Null(path);
    }

    [Fact]
    public void Resolve_Returns_Tauri_Supplied_Path()
    {
        var resourcePath = Path.Combine(_tempDirectory, "secrets.json");

        Assert.Equal(resourcePath, ShortagesSecretFilePath.Resolve(resourcePath));
    }

    [Fact]
    public void TryLoad_Returns_False_When_Tauri_Does_Not_Supply_A_Path()
    {
        var loaded = new ShortagesSecretFileLoader().TryLoad(out var settings);

        Assert.False(loaded);
        Assert.Null(settings);
    }

    [Fact]
    public void TryLoad_Loads_Valid_Placeholder_Configuration()
    {
        var path = WriteSecretFile(ValidSecretJson());

        var loaded = new ShortagesSecretFileLoader(path).TryLoad(out var settings);

        Assert.True(loaded);
        Assert.NotNull(settings);
        Assert.Equal("TESTSERVER", settings.Server);
        Assert.Equal("TESTDB", settings.Database);
        Assert.True(settings.Encrypt);
        Assert.False(settings.TrustServerCertificate);
    }

    [Theory]
    [InlineData("missing.json")]
    [InlineData("malformed.json")]
    [InlineData("missing-field.json")]
    public void TryLoad_Returns_False_For_Missing_Invalid_Or_Incomplete_File(string fileName)
    {
        var path = Path.Combine(_tempDirectory, fileName);
        if (fileName == "malformed.json")
            File.WriteAllText(path, "{ invalid json");
        else if (fileName == "missing-field.json")
            File.WriteAllText(path, "{ \"Shortages\": { \"Server\": \"TESTSERVER\", \"Database\": \"TESTDB\", \"Username\": \"test-user\", \"Password\": \" \" } }");

        var loaded = new ShortagesSecretFileLoader(path).TryLoad(out var settings);

        Assert.False(loaded);
        Assert.Null(settings);
    }

    [Fact]
    public void Build_Uses_Sql_Authentication_And_Secure_Default_Transport()
    {
        var settings = LoadValidSettings();

        var builder = new SqlConnectionStringBuilder(ShortagesConnectionStringFactory.Build(settings));

        Assert.False(builder.IntegratedSecurity);
        Assert.Equal("test-user", builder.UserID);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, builder.Encrypt);
        Assert.False(builder.TrustServerCertificate);
    }

    [Fact]
    public void Build_Uses_Explicit_Transport_Values_Without_Integrated_Security_Fallback()
    {
        var settings = LoadValidSettings() with { Encrypt = false, TrustServerCertificate = true };

        var builder = new SqlConnectionStringBuilder(ShortagesConnectionStringFactory.Build(settings));

        Assert.Equal(SqlConnectionEncryptOption.Optional, builder.Encrypt);
        Assert.True(builder.TrustServerCertificate);
        Assert.False(builder.IntegratedSecurity);
    }

    [Fact]
    public void Bundled_Example_Is_Placeholder_Only()
    {
        var repositoryRoot = FindRepositoryRoot();
        var examplePath = Path.Combine(repositoryRoot, "src", "tauri", "resources", "secrets.example.json");
        var contents = File.ReadAllText(examplePath);

        Assert.Contains("\"Username\": \"<dedicated read-only SQL login>\"", contents);
        Assert.Contains("\"Password\": \"<secret>\"", contents);
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
                return current.FullName;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

    private ShortagesSecretFileLoader.ShortagesSqlConnectionSettings LoadValidSettings()
    {
        var loaded = new ShortagesSecretFileLoader(WriteSecretFile(ValidSecretJson())).TryLoad(out var settings);
        Assert.True(loaded);
        return Assert.IsType<ShortagesSecretFileLoader.ShortagesSqlConnectionSettings>(settings);
    }

    private string WriteSecretFile(string contents)
    {
        var path = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, contents);
        return path;
    }

    private static string ValidSecretJson() => """
        {
          "Shortages": {
            "Server": "TESTSERVER",
            "Database": "TESTDB",
            "Username": "test-user",
            "Password": "test-password",
            "Encrypt": true,
            "TrustServerCertificate": false
          }
        }
        """;
}

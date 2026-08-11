using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Kst.ArchitectureTests;

/// <summary>
/// Guards against version drift between the authoritative .NET version source
/// (src/backend/Directory.Build.props) and the Tauri/frontend version-bearing files that must be
/// kept manually in sync via scripts/check-version.ps1. See docs/development/VERSIONING.md.
/// </summary>
public sealed class VersionConsistencyTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "global.json")))
            dir = dir.Parent;

        if (dir is null)
            throw new InvalidOperationException("Could not locate repository root (global.json not found).");

        return dir.FullName;
    }

    private static (string Prefix, string Suffix) ReadAuthoritativeVersionParts(string repoRoot)
    {
        var props = File.ReadAllText(Path.Combine(repoRoot, "src", "backend", "Directory.Build.props"));
        var prefix = Regex.Match(props, "<VersionPrefix>(.*?)</VersionPrefix>").Groups[1].Value;
        var suffix = Regex.Match(props, "<VersionSuffix>(.*?)</VersionSuffix>").Groups[1].Value;

        prefix.Should().NotBeNullOrEmpty("Directory.Build.props must define <VersionPrefix>");
        suffix.Should().NotBeNullOrEmpty("Directory.Build.props must define <VersionSuffix>");

        return (prefix, suffix);
    }

    private static string ReadAuthoritativeVersion(string repoRoot)
    {
        var (prefix, suffix) = ReadAuthoritativeVersionParts(repoRoot);
        return $"{prefix}-{suffix}";
    }

    [Fact]
    public void Tauri_CargoToml_Version_Matches_Authoritative_Version()
    {
        var repoRoot = FindRepoRoot();
        var authoritative = ReadAuthoritativeVersion(repoRoot);

        var cargoToml = File.ReadAllText(Path.Combine(repoRoot, "src", "tauri", "Cargo.toml"));
        var cargoVersion = Regex.Match(cargoToml, "^version = \"(.*?)\"", RegexOptions.Multiline).Groups[1].Value;

        cargoVersion.Should().Be(authoritative,
            because: "src/tauri/Cargo.toml must stay in sync with the authoritative version " +
                     "in Directory.Build.props (run scripts/check-version.ps1 -Fix)");
    }

    [Fact]
    public void Tauri_Conf_Json_Version_Matches_Numeric_Authoritative_Version()
    {
        var repoRoot = FindRepoRoot();
        var (prefix, _) = ReadAuthoritativeVersionParts(repoRoot);

        var confJson = File.ReadAllText(Path.Combine(repoRoot, "src", "tauri", "tauri.conf.json"));
        using var doc = JsonDocument.Parse(confJson);
        var version = doc.RootElement.GetProperty("version").GetString();

        // tauri.conf.json's "version" drives the Windows MSI/WiX installer's ProductVersion,
        // which only accepts numeric (major.minor.build) versions - a non-numeric SemVer
        // pre-release identifier like "-alpha.1" fails MSI bundling ("optional pre-release
        // identifier ... must be numeric-only"). This is deliberately numeric-only (VersionPrefix,
        // no suffix) even though the real application-reported version (backend InformationalVersion,
        // system status, top bar) keeps the full pre-release string. See docs/development/VERSIONING.md.
        version.Should().Be(prefix,
            because: "src/tauri/tauri.conf.json must stay in sync with the numeric-only " +
                     "VersionPrefix in Directory.Build.props (run scripts/check-version.ps1 -Fix) - " +
                     "MSI/WiX bundling rejects non-numeric pre-release identifiers");
    }

    [Fact]
    public void Frontend_PackageJson_Version_Matches_Authoritative_Version()
    {
        var repoRoot = FindRepoRoot();
        var authoritative = ReadAuthoritativeVersion(repoRoot);

        var packageJson = File.ReadAllText(Path.Combine(repoRoot, "src", "frontend", "package.json"));
        using var doc = JsonDocument.Parse(packageJson);
        var version = doc.RootElement.GetProperty("version").GetString();

        version.Should().Be(authoritative,
            because: "src/frontend/package.json must stay in sync with the authoritative version " +
                     "in Directory.Build.props (run scripts/check-version.ps1 -Fix)");
    }
}

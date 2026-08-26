using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Regression coverage for S0.3-G002 (accepted S0.2/S0.3 evidence:
/// <c>docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md</c> §11; declared property in
/// <c>docs/security/SECURITY_BASELINE.md</c> §9 and <c>APPLICATION_SECURITY_PROFILE.md</c>).
///
/// Security invariant: the KST local ASP.NET Core backend must not become network-accessible by
/// accidentally binding to a wildcard or non-loopback interface through normal repository
/// configuration. The repository-controlled startup path in <c>Kst.Api/Program.cs</c> binds
/// <c>http://127.0.0.1:{port}</c> (port from <c>--port</c>, <c>KST_PORT</c>, or OS-assigned 0)
/// whenever <c>ASPNETCORE_URLS</c> is not set; the Tauri host launches the sidecar without setting
/// <c>ASPNETCORE_URLS</c>, so that fallback is the effective desktop-architecture binding.
///
/// Mechanism: this test launches the built <c>Kst.Api</c> host process exactly the way the
/// repository starts it (no <c>ASPNETCORE_URLS</c>, <c>--port=0</c>), reads the documented startup
/// handshake from stdout, then inspects the OS TCP listener table and asserts the socket is bound
/// to loopback (<c>127.0.0.1</c>). It is a behavioral check of the actual startup path — it fails
/// if the effective binding becomes <c>0.0.0.0</c>, <c>::</c>, <c>*</c>/<c>+</c>, or a LAN address —
/// and survives harmless refactoring of how the URL is constructed.
///
/// Scope limits (recorded, not asserted here): an operator-set <c>ASPNETCORE_URLS</c> environment
/// variable is a documented, operator-controlled override (SETUP.md default
/// <c>http://127.0.0.1:0</c>) outside repository configuration; packaged/installed runtime
/// listener behavior remains S0.7. QAD is forced unconfigured in the child process, so no database
/// connection is made; only the local socket bind is observed.
/// </summary>
public sealed class LoopbackBindingTests : IDisposable
{
    private const int HandshakeTimeoutSeconds = 30;
    private const int ListenerPollTimeoutSeconds = 15;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private Process? _process;

    [Fact]
    public async Task Backend_Process_Binds_To_Loopback_Only()
    {
        // Kst.Api is built self-contained (win-x64) — the same shape the Tauri sidecar launches —
        // so run the real Kst.Api.exe from Kst.Api's own build output (framework-dependent
        // 'dotnet Kst.Api.dll' cannot run because its runtimeconfig is self-contained).
        var apiDirectory = FindApiOutputDirectory();
        var apiExecutable = Path.Combine(apiDirectory, "Kst.Api.exe");
        Assert.True(
            File.Exists(apiExecutable),
            $"Kst.Api.exe was not found in {apiDirectory}; build the backend (dotnet build Kst.slnx) " +
            "before running this test.");

        var psi = new ProcessStartInfo
        {
            FileName = apiExecutable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            WorkingDirectory = apiDirectory,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("--port=0");

        // The repository-controlled binding path: the child must not inherit an ASPNETCORE_URLS
        // override (the documented operator override is outside this test's invariant).
        psi.Environment.Remove("ASPNETCORE_URLS");
        // Hermetic: force QAD/Shortages "not configured" so nothing but the socket bind occurs.
        psi.Environment["QadDatabase__Server"] = "";
        psi.Environment["QadDatabase__Database"] = "";
        psi.Environment["ShortagesDatabase__Server"] = "";
        psi.Environment["ShortagesDatabase__Database"] = "";
        psi.Environment["ASPNETCORE_CONTENTROOT"] = apiDirectory;

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start the Kst.Api host process.");
        _process = process;

        try
        {
            var port = await ReadHandshakePortAsync(process);
            Assert.True(
                port > 0,
                "The Kst.Api host did not emit the startup handshake {\"port\":...} within " +
                $"{HandshakeTimeoutSeconds}s; cannot verify the effective listener binding.");

            var endpoint = await WaitForTcpListenerAsync(port);
            Assert.True(
                endpoint is not null,
                $"No TCP listener was found on the handshake port {port} within " +
                $"{ListenerPollTimeoutSeconds}s; the backend did not start listening.");

            var boundAddress = endpoint!.Address;
            Assert.True(
                boundAddress.Equals(IPAddress.Loopback),
                "SECURITY REGRESSION (S0.3-G002): the KST backend bound to " +
                $"{boundAddress} — the local backend must bind to loopback (127.0.0.1) only; " +
                "a wildcard or non-loopback bind makes it network-accessible. " +
                "Review against docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md and " +
                "docs/security/SECURITY_BASELINE.md §9 before any intentional change.");
        }
        finally
        {
            StopBackend(process);
            _process = null;
        }
    }

    public void Dispose()
    {
        if (_process is not null)
        {
            StopBackend(_process);
            _process = null;
        }
    }

    /// <summary>
    /// Locates <c>Kst.Api/bin/&lt;configuration&gt;/net10.0/win-x64</c> (the self-contained build
    /// output) by walking up from the test output directory to the backend root and rejoining
    /// with the configuration this test build used.
    /// </summary>
    private static string FindApiOutputDirectory()
    {
        var backendRoot = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 10 && backendRoot is not null; depth++)
        {
            if (File.Exists(Path.Combine(backendRoot.FullName, "Kst.slnx")))
                break;
            backendRoot = backendRoot.Parent;
        }

        Assert.True(
            backendRoot is not null,
            $"Could not locate the backend root (directory containing Kst.slnx) from " +
            $"{AppContext.BaseDirectory}.");

        // .../tests/Kst.Api.IntegrationTests/bin/<configuration>/net10.0 -> <configuration>
        var configuration = AppContext.BaseDirectory
            .Split(Path.DirectorySeparatorChar)
            .Where(segment => segment.Length > 0)
            .ToList();
        var binIndex = configuration.IndexOf("bin");
        Assert.True(
            binIndex >= 0 && binIndex + 1 < configuration.Count,
            $"Could not determine the build configuration from the test output path {AppContext.BaseDirectory}.");
        var configName = configuration[binIndex + 1];

        return Path.Combine(backendRoot.FullName, "Kst.Api", "bin", configName, "net10.0", "win-x64");
    }

    private static async Task<int> ReadHandshakePortAsync(Process process)
    {
        var deadline = DateTime.UtcNow.AddSeconds(HandshakeTimeoutSeconds);
        while (DateTime.UtcNow < deadline && !process.HasExited)
        {
            string? line;
            try
            {
                line = await process.StandardOutput.ReadLineAsync();
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            if (line is null)
                break;

            // stdout also carries Serilog lines; only the handshake is a JSON object with a port.
            if (line.TrimStart().StartsWith('{'))
            {
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("port", out var portElement)
                        && portElement.TryGetInt32(out var port)
                        && port > 0)
                    {
                        return port;
                    }
                }
                catch (JsonException)
                {
                    // Not the handshake line; keep reading.
                }
            }
        }

        return 0;
    }

    private static async Task<IPEndPoint?> WaitForTcpListenerAsync(int port)
    {
        var deadline = DateTime.UtcNow.AddSeconds(ListenerPollTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            var match = listeners.FirstOrDefault(e => e.Port == port);
            if (match is not null)
                return match;

            await Task.Delay(200);
        }

        return null;
    }

    private static void StopBackend(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch
        {
            // Best-effort cleanup; the process is a child of this test and dies with it otherwise.
        }
        finally
        {
            process.Dispose();
        }
    }
}

using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Regression coverage for S0.3-G002 (accepted S0.2/S0.3 evidence:
/// <c>docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md</c> §11; declared property in
/// <c>docs/security/SECURITY_BASELINE.md</c> §9 and <c>APPLICATION_SECURITY_PROFILE.md</c>) and
/// for the S0.5-F001 / S0.7 loopback-binding invariant remediation (recorded in
/// <c>docs/security/S0_7_RUNTIME_INFRASTRUCTURE_VERIFICATION.md</c>, remediation section).
///
/// Security invariant: the KST local ASP.NET Core backend must listen only on the loopback
/// interface — as an application-controlled invariant. <c>Kst.Api/Program.cs</c> unconditionally
/// supplies its own explicit loopback endpoint via
/// <c>UseUrls("http://127.0.0.1:{port}")</c> (port from <c>--port</c>, <c>KST_PORT</c>, or
/// OS-assigned 0) — never a fallback taken only when <c>ASPNETCORE_URLS</c> happens to be absent
/// (the pre-fix behavior, which let an inherited <c>ASPNETCORE_URLS</c> value take authority over
/// the listener). On the shipped self-contained .NET 10 release runtime, that explicit endpoint
/// was behaviorally verified to take effect over an inherited <c>ASPNETCORE_URLS</c> value
/// (S0.7A re-verification, 2026-08-28); these regression tests keep the invariant under
/// continuous protection.
///
/// Mechanism: tests (A) and (B) launch the built <c>Kst.Api</c> host process exactly the way the
/// repository starts it (<c>--port=0</c>, QAD/Shortages forced unconfigured), optionally with an
/// inherited <c>ASPNETCORE_URLS</c> value, read the documented startup handshake from stdout,
/// then inspect the OS TCP listener table and assert the effective socket is bound to loopback
/// (<c>127.0.0.1</c>). Test (C) is a non-listening configuration-level check: it starts the KST
/// host in the in-memory test host (no socket is ever opened, in any code path) and asserts the
/// host supplies exactly one explicit <c>127.0.0.1</c> endpoint even with an inherited
/// <c>ASPNETCORE_URLS</c> present. All three fail if the effective binding becomes
/// <c>0.0.0.0</c>, <c>::</c>, <c>*</c>/<c>+</c>, or a LAN address, and survive harmless
/// refactoring of how the URL is constructed.
///
/// Failure-safe by construction (S0.7A steering correction, 2026-08-28): no test in this class
/// may create a wildcard or externally reachable listener on this workstation — not even in its
/// failing state. Test (B) therefore simulates the inherited configuration with a loopback-only
/// sentinel: if the invariant were broken, the child would at worst bind that loopback address.
/// The original version of test (C) launched the real process with
/// <c>ASPNETCORE_URLS=http://0.0.0.0:&lt;port&gt;</c>; it was replaced before acceptance because a
/// future regression could have made the failing security test itself expose a real wildcard
/// listener. The safe alternate-loopback test (B) preserves the behavioral coverage — inherited
/// URL configuration cannot take authority over KST's selected endpoint.
///
/// Pre-remediation failure relationship (recorded; the experiment was not repeated at the
/// 2026-08-28 steering correction): before the fix, <c>Program.cs</c> skipped <c>UseUrls</c>
/// whenever <c>ASPNETCORE_URLS</c> was present, so inherited hosting configuration took
/// authority over the listener (reproduced at release runtime in the S0.7A pass). Test (B) was
/// demonstrated to fail against that pre-fix build on 2026-08-28 (the child honored the env
/// port); test (C) fails against that pre-fix behavior by construction (the test host would then
/// report the inherited sentinel as the configured endpoint).
///
/// Scope limits (recorded, not asserted here): packaged/installed runtime listener behavior
/// remains S0.7 (runtime re-verification, not a repository test). QAD is forced unconfigured in
/// the child process, so no database connection is made; only the local socket bind is observed.
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
        // (A) Normal effective configuration (no inherited ASPNETCORE_URLS): the effective
        // listener must be loopback-only, on the OS-assigned dynamic port.
        var (process, port) = await StartBackendAndReadHandshakePortAsync(aspnetcoreUrls: null);
        try
        {
            Assert.True(
                port > 0,
                "The Kst.Api host did not emit the startup handshake {\"port\":...} within " +
                $"{HandshakeTimeoutSeconds}s; cannot verify the effective listener binding.");

            AssertEffectiveListenerIsLoopback(port);
        }
        finally
        {
            StopBackend(process);
            _process = null;
        }
    }

    [Fact]
    public async Task Backend_Inherited_AspnetcoreUrls_LoopbackPort_Does_Not_Take_Authority()
    {
        // (B) An inherited ASPNETCORE_URLS using a different loopback port must not take
        // authority over KST's listener selection (S0.5-F001 remediation invariant). The
        // effective listener must remain the KST-selected loopback binding (OS-assigned port),
        // not the environment-provided port.
        var envPort = GetFreeLoopbackPort();
        var (process, port) = await StartBackendAndReadHandshakePortAsync(
            $"http://127.0.0.1:{envPort}");
        try
        {
            Assert.True(
                port > 0,
                "The Kst.Api host did not emit the startup handshake {\"port\":...} within " +
                $"{HandshakeTimeoutSeconds}s; cannot verify the effective listener binding.");

            Assert.True(
                port != envPort,
                "SECURITY REGRESSION (S0.5-F001): the effective listener port " +
                $"{port} equals the inherited ASPNETCORE_URLS port {envPort} — inherited " +
                "hosting configuration must not take authority over KST's loopback binding. " +
                "KST must bind the explicit 127.0.0.1 endpoint it selects itself.");

            AssertEffectiveListenerIsLoopback(port);
        }
        finally
        {
            StopBackend(process);
            _process = null;
        }
    }

    [Fact]
    public void Host_Endpoint_Selection_Supplies_Only_Explicit_Loopback_Endpoint()
    {
        // (C) Non-listening configuration-level check: the KST host must unconditionally
        // supply exactly one explicit 127.0.0.1 endpoint via UseUrls — even with an inherited
        // ASPNETCORE_URLS value present. The test host is in-memory (TestServer), so no socket
        // is opened in any code path and this assertion remains safe even if the loopback
        // invariant is broken. The sentinel is loopback-only (127.0.0.0/8 is loopback) as a
        // second layer of failure safety. (This replaces the original real-process wildcard
        // version of this test; see the type-level summary for why.)
        const string sentinel = "http://127.0.0.2:19999";
        var previous = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", sentinel);
        try
        {
            using var factory = new KstApiFactory();
            var addressesFeature = factory.Server.Features.Get<IServerAddressesFeature>();
            Assert.True(
                addressesFeature is not null,
                "Could not inspect the KST test host's configured server addresses.");

            var addresses = addressesFeature!.Addresses.ToArray();
            Assert.True(
                addresses.Length == 1,
                "SECURITY REGRESSION (S0.5-F001): the KST host configured " +
                $"{addresses.Length} server addresses ({string.Join(", ", addresses)}) — KST " +
                "must supply exactly one explicit endpoint for its local backend listener.");

            var address = addresses[0];
            Assert.True(
                address.StartsWith("http://127.0.0.1:", StringComparison.Ordinal),
                "SECURITY REGRESSION (S0.3-G002 / S0.5-F001): the KST host configured server " +
                $"address '{address}' — the endpoint must be the explicit loopback address " +
                "127.0.0.1; a wildcard, non-loopback, or inherited address breaks the local " +
                "backend's loopback-only invariant.");

            Assert.True(
                !string.Equals(address, sentinel, StringComparison.Ordinal),
                "SECURITY REGRESSION (S0.5-F001): the configured server address equals the " +
                "inherited ASPNETCORE_URLS sentinel — inherited hosting configuration must " +
                "not take authority over KST's explicit endpoint selection.");
        }
        finally
        {
            // Restore the ambient value so the sentinel cannot leak into other tests (the test
            // host is in-memory, so even a leak could not open a listener).
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", previous);
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
    /// Starts the real <c>Kst.Api</c> host process the way the repository starts it
    /// (<c>--port=0</c>; QAD/Shortages forced unconfigured; content root set to the build output)
    /// and reads the startup handshake port. When <paramref name="aspnetcoreUrls"/> is non-null
    /// it is set on the child process environment to simulate inherited hosting configuration;
    /// when null it is removed so the child cannot inherit an ambient override.
    /// </summary>
    private async Task<(Process Process, int Port)> StartBackendAndReadHandshakePortAsync(
        string? aspnetcoreUrls)
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

        if (aspnetcoreUrls is null)
            psi.Environment.Remove("ASPNETCORE_URLS");
        else
            psi.Environment["ASPNETCORE_URLS"] = aspnetcoreUrls;

        // Hermetic: force QAD/Shortages "not configured" so nothing but the socket bind occurs.
        psi.Environment["QadDatabase__Server"] = "";
        psi.Environment["QadDatabase__Database"] = "";
        psi.Environment["ShortagesDatabase__Server"] = "";
        psi.Environment["ShortagesDatabase__Database"] = "";
        psi.Environment["ASPNETCORE_CONTENTROOT"] = apiDirectory;

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start the Kst.Api host process.");
        _process = process;

        return (process, await ReadHandshakePortAsync(process));
    }

    /// <summary>
    /// Asserts that a TCP listener exists on <paramref name="port"/> and that it is bound to
    /// loopback (<c>127.0.0.1</c>) — the declared security invariant for the KST backend.
    /// </summary>
    private static void AssertEffectiveListenerIsLoopback(int port)
    {
        var endpoint = WaitForTcpListenerSync(port);
        Assert.True(
            endpoint is not null,
            $"No TCP listener was found on the handshake port {port} within " +
            $"{ListenerPollTimeoutSeconds}s; the backend did not start listening.");

        var boundAddress = endpoint!.Address;
        Assert.True(
            boundAddress.Equals(IPAddress.Loopback),
            "SECURITY REGRESSION (S0.3-G002 / S0.5-F001): the KST backend bound to " +
            $"{boundAddress} — the local backend must bind to loopback (127.0.0.1) only; " +
            "a wildcard or non-loopback bind makes it network-accessible. " +
            "Review against docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md and " +
            "docs/security/SECURITY_BASELINE.md §9 before any intentional change.");
    }

    /// <summary>
    /// Returns the OS TCP listener entry on <paramref name="port"/> (any address) or null.
    /// </summary>
    private static IPEndPoint? FindTcpListenerOnPort(int port)
    {
        return IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .FirstOrDefault(e => e.Port == port);
    }

    /// <summary>
    /// Polls the OS TCP listener table until a listener appears on <paramref name="port"/> or
    /// the timeout elapses.
    /// </summary>
    private static IPEndPoint? WaitForTcpListenerSync(int port)
    {
        var deadline = DateTime.UtcNow.AddSeconds(ListenerPollTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var match = FindTcpListenerOnPort(port);
            if (match is not null)
                return match;

            Thread.Sleep(200);
        }

        return FindTcpListenerOnPort(port);
    }

    /// <summary>
    /// Obtains a currently free port on the loopback interface by briefly binding it, so tests
    /// can simulate inherited URL values without colliding with existing listeners. (The
    /// release-and-rebind window is acceptable for this local, single-test-suite context; a
    /// collision would only make a test observe a coincidental port match, which the loopback
    /// address assertion still covers.)
    /// </summary>
    private static int GetFreeLoopbackPort()
    {
        var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        try
        {
            return ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
        }
        finally
        {
            probe.Stop();
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

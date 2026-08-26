using System.Net;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kst.Api.IntegrationTests;

public sealed class CorsPolicyTests : IClassFixture<KstApiFactory>
{
    /// <summary>The applied CORS policy name (see <c>Kst.Api/Program.cs</c> and S0.2 baseline §10).</summary>
    private const string FrontendPolicyName = "FrontendPolicy";

    /// <summary>
    /// The accepted S0 CORS surface (S0.2 baseline §10; S0.3 evidence §6.3): exactly these five
    /// frontend/Tauri origins, no AllowAnyOrigin, no credentials, AllowAnyHeader + AllowAnyMethod.
    /// This list is the *accepted policy* restated as test authority — it is not read from
    /// Program.cs, so an accidental broadening or removal in production code fails these tests.
    /// </summary>
    private static readonly string[] AcceptedAllowedOrigins =
    [
        "http://localhost:1420",
        "http://127.0.0.1:1420",
        "tauri://localhost",
        "http://tauri.localhost",
        "https://tauri.localhost"
    ];

    private readonly HttpClient _client;
    private readonly KstApiFactory _factory;

    public CorsPolicyTests(KstApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_WithAllowedOrigin_ReturnsCorsHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://localhost:1420");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal("http://localhost:1420", values.Single());
    }

    [Fact]
    public async Task GetHealth_WithPackagedTauriOrigin_ReturnsCorsHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://tauri.localhost");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal("http://tauri.localhost", values.Single());
    }

    // S0.5 (S0.3 secondary observation): the pre-S0.5 tests covered 2 of the 5 accepted origins.

    [Theory]
    [InlineData("http://localhost:1420")]
    [InlineData("http://127.0.0.1:1420")]
    [InlineData("tauri://localhost")]
    [InlineData("http://tauri.localhost")]
    [InlineData("https://tauri.localhost")]
    public async Task GetHealth_WithEveryAcceptedOrigin_ReturnsCorsHeaderEchoingThatOrigin(string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", origin);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values),
            $"Accepted origin {origin} was not echoed in Access-Control-Allow-Origin — an accepted " +
            "origin was removed from the CORS policy (review against the S0.2 baseline §10 accepted set).");
        Assert.Equal(origin, values.Single());
    }

    [Fact]
    public async Task GetHealth_WithUntrustedOrigin_DoesNotReceiveAllowOriginHeader()
    {
        const string untrustedOrigin = "https://untrusted.example.com";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", untrustedOrigin);

        using var response = await _client.SendAsync(request);

        var hasHeader = response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values);
        var echoedOrigin = values is null ? "(header present)" : string.Join(", ", values);
        Assert.False(
            hasHeader,
            $"Untrusted origin {untrustedOrigin} received Access-Control-Allow-Origin={echoedOrigin} — " +
            "the CORS policy was broadened beyond the accepted origin set (e.g. AllowAnyOrigin). " +
            "Review against the S0.2 baseline §10 before any intentional change.");
    }

    /// <summary>
    /// Structural assertion on the *effective registered* CORS configuration (not the response
    /// headers): the applied frontend policy (named <c>FrontendPolicy</c> in
    /// <c>Kst.Api/Program.cs</c>, recorded in the S0.2 baseline §10) must carry exactly the five
    /// accepted origins, no AllowAnyOrigin, and no credentials. Fails on any broadening (a sixth
    /// origin, a wildcard origin, or credentials) or on removal of an accepted origin. The
    /// response-level behavior is separately covered by the echo/rejection tests above, which
    /// exercise the applied middleware end to end.
    /// </summary>
    [Fact]
    public void Effective_Cors_Configuration_Matches_Accepted_S0_Surface()
    {
        // AddCors registers the configuration through options; the applied UseCors policy
        // resolves this same instance at request time.
        var corsOptions = _factory.Services.GetRequiredService<IOptions<CorsOptions>>().Value;

        // GetPolicy resolves the same configuration the applied UseCors("FrontendPolicy")
        // middleware uses at request time.
        var policy = corsOptions.GetPolicy(FrontendPolicyName);

        Assert.True(
            policy is not null,
            $"The applied CORS policy '{FrontendPolicyName}' is no longer registered — the accepted " +
            "frontend CORS surface (S0.2 baseline §10) must remain a single named policy.");
        var effectivePolicy = policy!;


        var expectedOrigins = AcceptedAllowedOrigins.OrderBy(o => o, StringComparer.Ordinal).ToArray();
        var actualOrigins = effectivePolicy.Origins.OrderBy(o => o, StringComparer.Ordinal).ToArray();
        Assert.True(
            expectedOrigins.SequenceEqual(actualOrigins),
            "The CORS allowed-origin set drifted from the accepted S0 origin set — an origin was " +
            $"added (broadening) or removed. Actual: [{string.Join(", ", actualOrigins)}]. Review against " +
            "the S0.2 baseline §10 before any intentional change.");

        Assert.False(
            effectivePolicy.AllowAnyOrigin,
            "CORS policy enables AllowAnyOrigin — every origin is accepted, which is outside the " +
            "accepted desktop CORS architecture.");

        Assert.False(
            effectivePolicy.SupportsCredentials,
            "CORS policy enables credentials; the accepted KST architecture does not require CORS " +
            "credentials (no cookie/session cross-origin access is part of the desktop flow). If this " +
            "is a deliberate architecture change, surface it before changing the assertion.");

        Assert.True(
            effectivePolicy.AllowAnyHeader,
            "AllowAnyHeader was removed from the accepted frontend CORS policy — review against the " +
            "S0.2 baseline §10 accepted surface.");

        Assert.True(
            effectivePolicy.AllowAnyMethod,
            "AllowAnyMethod was removed from the accepted frontend CORS policy — review against the " +
            "S0.2 baseline §10 accepted surface.");
    }
}

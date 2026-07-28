using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kst.Api.IntegrationTests;

/// <summary>
/// Test factory that configures the API to use a random loopback port,
/// suppresses external dependencies, and runs without a real database.
/// </summary>
public sealed class KstApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // No overrides needed — the placeholder integrations are already
            // registered when QAD / Shortages config is absent.
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Events;
using Kst.Api.Endpoints;
using Kst.Application.Mps;
using Kst.Application.PartDetail;
using Kst.Application.Preferences;
using Kst.Application.Refresh;
using Kst.Application.SystemStatus;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Application.Snapshots;
using Kst.Infrastructure;
using Kst.Infrastructure.Configuration;
using Kst.Infrastructure.Identity;
using Kst.Infrastructure.Mps;
using Kst.Infrastructure.PartDetail;
using Kst.Infrastructure.SystemStatus;
using Kst.Integrations.Qad.Connectivity;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;
using Kst.Integrations.Qad.PartDetail;
using Kst.Integrations.Shortages.Connectivity;
using Kst.Integrations.Shortages.Options;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendPolicy";

// -- Logging -------------------------------------------------------------------
var paths = new LocalAppDataPaths();
try { paths.EnsureDirectoriesExist(); } catch { /* non-fatal; file logging disabled */ }

builder.Services.AddSerilog((services, cfg) =>
{
    cfg.ReadFrom.Configuration(builder.Configuration)
       .Enrich.FromLogContext()
       .Enrich.WithProperty("InstanceId", ApplicationInstanceId.Value)
       .WriteTo.Console(
           restrictedToMinimumLevel: LogEventLevel.Debug,
           outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{InstanceId}] {Message:lj}{NewLine}{Exception}")
       .WriteTo.File(
           Path.Combine(paths.LogsDirectory, "kst-.log"),
           rollingInterval: RollingInterval.Day,
           retainedFileCountLimit: 14,
           outputTemplate:
               "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{InstanceId}] {Message:lj}{NewLine}{Exception}");
});

// -- Binding: loopback only ----------------------------------------------------
// Tauri sidecar manager sets ASPNETCORE_URLS=http://127.0.0.1:<port>.
// Fall back to OS-assigned loopback port when the env var is absent.
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    var portArg = args.FirstOrDefault(a => a.StartsWith("--port="))?.Split('=')[1]
               ?? args.SkipWhile(a => a != "--port").Skip(1).FirstOrDefault()
               ?? builder.Configuration["KST_PORT"];

    var listenPort = 0;
    if (!string.IsNullOrWhiteSpace(portArg) && int.TryParse(portArg, out var p))
        listenPort = p;

    builder.WebHost.UseUrls($"http://127.0.0.1:{listenPort}");
}

// -- Configuration -------------------------------------------------------------
var qadOptions = builder.Configuration
    .GetSection(QadConnectionOptions.SectionName)
    .Get<QadConnectionOptions>() ?? new QadConnectionOptions();

var shortagesOptions = builder.Configuration
    .GetSection(ShortagesConnectionOptions.SectionName)
    .Get<ShortagesConnectionOptions>() ?? new ShortagesConnectionOptions();

// -- Services ------------------------------------------------------------------
builder.Services.AddInfrastructure();

builder.Services.AddSingleton(qadOptions);
if (qadOptions.IsConfigured)
    builder.Services.AddSingleton<IQadConnectivityCheck, SqlServerQadConnectivityCheck>();
else
    builder.Services.AddSingleton<IQadConnectivityCheck, DisabledQadConnectivityCheck>();
builder.Services.AddSingleton(shortagesOptions);
builder.Services.AddSingleton<IShortagesConnectivityCheck, DisabledShortagesConnectivityCheck>();

var appVersion = builder.Configuration["AppVersion"] ?? "0.1.0";
var startedAt = DateTimeOffset.Now;

builder.Services.AddSingleton(new ApplicationInfo(
    Name: "Keytronic Scheduler's Toolbox",
    Version: appVersion,
    InstanceId: ApplicationInstanceId.Value,
    StartedAt: startedAt
));

builder.Services.AddSingleton<IDataSourceStatusStore>(_ => new InMemoryDataSourceStatusStore(
[
    new DataSourceSummary("QAD",
        qadOptions.IsConfigured ? DataSourceStatus.Loading : DataSourceStatus.NotConfigured),
    new DataSourceSummary("Shortage Database",
        shortagesOptions.IsConfigured ? DataSourceStatus.Loading : DataSourceStatus.NotConfigured)
]));

builder.Services.AddSingleton<IReadOnlyList<IRefreshProvider>>(sp =>
[
    new DelegateRefreshProvider("QAD", async ct =>
    {
        var result = await sp.GetRequiredService<IQadConnectivityCheck>().CheckAsync(ct);
        return result.Status switch
        {
            ConnectivityStatus.NotConfigured => RefreshProviderOutcome.NotConfigured,
            ConnectivityStatus.Succeeded => RefreshProviderOutcome.Succeeded,
            ConnectivityStatus.Failed or ConnectivityStatus.TimedOut => RefreshProviderOutcome.Failed,
            _ => RefreshProviderOutcome.Unavailable
        };
    }),
    new DelegateRefreshProvider("Shortage Database", async ct =>
    {
        var result = await sp.GetRequiredService<IShortagesConnectivityCheck>().CheckAsync(ct);
        return result.Status switch
        {
            ShortagesConnectivityStatus.NotConfigured => RefreshProviderOutcome.NotConfigured,
            ShortagesConnectivityStatus.Succeeded => RefreshProviderOutcome.Succeeded,
            ShortagesConnectivityStatus.Failed or ShortagesConnectivityStatus.TimedOut => RefreshProviderOutcome.Failed,
            _ => RefreshProviderOutcome.Unavailable
        };
    })
]);

builder.Services.AddSingleton(sp => new RefreshCoordinator(
    sp.GetRequiredService<IClock>(),
    sp.GetRequiredService<ISnapshotStore>(),
    sp.GetRequiredService<IDataSourceStatusStore>(),
    sp.GetRequiredService<IRefreshHistoryStore>(),
    sp.GetRequiredService<IReadOnlyList<IRefreshProvider>>()));

builder.Services.AddScoped<GetSystemStatusQuery>();
builder.Services.AddSingleton<IWorkspaceConfigurationService, WorkspaceConfigurationService>();
builder.Services.AddSingleton<IPreferencesService, PreferencesService>();

// -- MPS (Stage 5B) --------------------------------------------------------
builder.Services.AddSingleton<IMpsSnapshotStore, InMemoryMpsSnapshotStore>();

if (qadOptions.IsConfigured)
{
    builder.Services.AddSingleton<QadMpsSourceReader>();
    builder.Services.AddSingleton<QadMpsScopeResolver>();
    builder.Services.AddSingleton<IMpsSourceReader>(sp => new DelegateMpsSourceReader(
        (site, parentParts, ct) => sp.GetRequiredService<QadMpsSourceReader>().ReadAsync(site, parentParts, ct)));
    builder.Services.AddSingleton<IMpsScopeResolver>(sp => new DelegateMpsScopeResolver(
        (workspace, ct) => sp.GetRequiredService<QadMpsScopeResolver>().ResolveAsync(workspace, ct)));
}
else
{
    const string notConfiguredMessage = "QAD connection is not configured.";
    builder.Services.AddSingleton<IMpsSourceReader>(_ => new DelegateMpsSourceReader(
        (_, _, _) => throw new InvalidOperationException(notConfiguredMessage)));
    builder.Services.AddSingleton<IMpsScopeResolver>(_ => new DelegateMpsScopeResolver(
        (_, _) => throw new InvalidOperationException(notConfiguredMessage)));
}

builder.Services.AddSingleton<MpsWorkspaceSnapshotService>();

// -- Part Detail (Stage 6) -------------------------------------------------
builder.Services.AddSingleton<IPartDetailCacheStore, InMemoryPartDetailCacheStore>();

if (qadOptions.IsConfigured)
{
    builder.Services.AddSingleton<QadPartDetailReader>();
    builder.Services.AddSingleton<IPartDetailSourceReader>(sp => new DelegatePartDetailSourceReader(
        (site, partNumber, today, ct) => sp.GetRequiredService<QadPartDetailReader>().ReadAsync(site, partNumber, today, ct)));
}
else
{
    const string notConfiguredMessage = "QAD connection is not configured.";
    builder.Services.AddSingleton<IPartDetailSourceReader>(_ => new DelegatePartDetailSourceReader(
        (_, _, _, _) => throw new InvalidOperationException(notConfiguredMessage)));
}

builder.Services.AddSingleton<PartDetailService>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:1420",
                "http://127.0.0.1:1420",
                "tauri://localhost",
                "http://tauri.localhost",
                "https://tauri.localhost")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});

// -- Build ---------------------------------------------------------------------
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("KST backend starting. Version={Version} InstanceId={InstanceId}",
    appVersion, ApplicationInstanceId.Value);

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(FrontendCorsPolicy);
app.MapOpenApi();

app.MapDiagnosticEndpoints();
app.MapSystemEndpoints();
app.MapWorkspaceEndpoints();
app.MapPreferencesEndpoints();
app.MapMpsEndpoints();
app.MapPartDetailEndpoints();

// -- Startup handshake ---------------------------------------------------------
// Writes a JSON line to stdout once the server is bound so Tauri can read the port.
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var address = app.Urls.FirstOrDefault() ?? "http://127.0.0.1:0";
        var actualPort = new Uri(address).Port;
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            port = actualPort,
            instanceId = ApplicationInstanceId.Value,
            status = "starting"
        }));
        Console.Out.Flush();
        logger.LogInformation("KST backend listening on {Address} (port {Port})", address, actualPort);
    }
    catch
    {
        // Expected during build-time OpenAPI document generation.
    }
});

await app.RunAsync();

// Expose Program class to test projects (WebApplicationFactory requires this)
public partial class Program { }

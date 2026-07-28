using Kst.Integrations.Shortages.Options;

namespace Kst.Integrations.Shortages.Connectivity;

/// <summary>
/// Placeholder that returns NotConfigured without attempting network calls.
/// </summary>
public sealed class DisabledShortagesConnectivityCheck : IShortagesConnectivityCheck
{
    private readonly ShortagesConnectionOptions _options;

    public DisabledShortagesConnectivityCheck(ShortagesConnectionOptions options)
    {
        _options = options;
    }

    public Task<ShortagesConnectivityResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ShortagesConnectivityResult(ShortagesConnectivityStatus.NotConfigured));
    }
}

using Kst.Integrations.Qad.Connectivity;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Connectivity;

/// <summary>
/// Placeholder connectivity check used when QAD is not yet configured.
/// Returns NotConfigured without attempting any network calls.
/// </summary>
public sealed class DisabledQadConnectivityCheck : IQadConnectivityCheck
{
    private readonly QadConnectionOptions _options;

    public DisabledQadConnectivityCheck(QadConnectionOptions options)
    {
        _options = options;
    }

    public Task<ConnectivityResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var result = _options.IsConfigured
            ? new ConnectivityResult(ConnectivityStatus.NotConfigured,
                "QAD connectivity check not yet implemented.")
            : new ConnectivityResult(ConnectivityStatus.NotConfigured);

        return Task.FromResult(result);
    }
}

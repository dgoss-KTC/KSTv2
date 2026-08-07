namespace Kst.Integrations.Qad.Mps;

/// <summary>
/// Infers the QAD domain from a workspace site. Domain is not a user-entered workspace field; the
/// QAD integration layer owns this mapping per the accepted Stage 5A site/domain table.
/// </summary>
public static class QadSiteDomainMap
{
    private static readonly IReadOnlyDictionary<string, string> SiteToDomain =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["NW"] = "KTC",
            ["SW"] = "KTC",
            ["AR"] = "KTC",
            ["MN"] = "KTC",
            ["MS"] = "KTC",
            ["KV"] = "KTV",
        };

    public static bool TryResolve(string site, out string domain)
    {
        if (SiteToDomain.TryGetValue(site, out var resolved))
        {
            domain = resolved;
            return true;
        }

        domain = string.Empty;
        return false;
    }

    public static string Resolve(string site) =>
        TryResolve(site, out var domain)
            ? domain
            : throw new InvalidOperationException($"No QAD domain mapping is configured for site '{site}'.");
}

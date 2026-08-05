namespace Kst.Domain.Workspaces;

/// <summary>
/// Conservative normalization for explicitly listed parent-level part numbers. Stage 4B does not
/// validate part numbers against QAD or impose canonicalization rules beyond whitespace/blank/duplicate
/// cleanup; authoritative canonicalization is deferred to Stage 5A.
/// </summary>
public static class ParentPartNormalizer
{
    /// <summary>
    /// Trims whitespace, drops blank entries, and removes exact duplicates while preserving the
    /// order of first occurrence. Case and internal characters (dashes, dots, spaces) are preserved.
    /// </summary>
    public static IReadOnlyList<string> Normalize(IEnumerable<string?>? rawParts)
    {
        if (rawParts is null)
            return [];

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();

        foreach (var raw in rawParts)
        {
            var trimmed = raw?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            if (seen.Add(trimmed))
                result.Add(trimmed);
        }

        return result;
    }

    /// <summary>
    /// Compares two normalized parent-part collections as sets — order and duplicates (already
    /// removed by <see cref="Normalize"/>) do not affect equality.
    /// </summary>
    public static bool SetEquals(IReadOnlyList<string> a, IReadOnlyList<string> b) =>
        new HashSet<string>(a, StringComparer.Ordinal).SetEquals(b);
}

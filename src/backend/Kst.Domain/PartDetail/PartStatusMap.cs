namespace Kst.Domain.PartDetail;

/// <summary>
/// Backend-owned QAD Part Status (<c>pt_status</c>) code-to-description mapping (accepted Stage 6
/// contract, section 5). An unrecognized code must not fail PartDetail: the raw code is always
/// preserved by the caller, and <see cref="Describe"/> simply returns null rather than inventing a
/// description.
/// </summary>
public static class PartStatusMap
{
    private static readonly IReadOnlyDictionary<string, string> Descriptions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = "AEMR",
            ["B"] = "BYPASS",
            ["C"] = "CURRENT",
            ["E"] = "END OF LIFE",
            ["F"] = "FORECAST",
            ["H"] = "PURCHASING HOLD",
            ["I"] = "INACTIVE PURCHASED PARTS",
            ["M"] = "MFA",
            ["N"] = "NPI",
            ["O"] = "OBSOLETE",
            ["P"] = "PROTO",
            ["Q"] = "QUOTED PARTS",
            ["U"] = "UNRELEASED",
        };

    /// <summary>Returns the known description for a raw status code, or null when unrecognized/blank.</summary>
    public static string? Describe(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        return Descriptions.TryGetValue(code.Trim(), out var description) ? description : null;
    }
}

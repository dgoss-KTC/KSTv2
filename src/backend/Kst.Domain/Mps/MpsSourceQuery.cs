namespace Kst.Domain.Mps;

/// <summary>
/// Fully resolved parameters for one MPS source read: an inferred QAD domain, a workspace site, and
/// the resolved parent-part scope. Contains no SQL-specific concepts.
/// </summary>
public sealed record MpsSourceQuery(
    string Domain,
    string Site,
    IReadOnlyList<string> ParentParts
);

namespace Kst.Domain.Mps;

/// <summary>
/// Selects which retained source date drives non-Falldown weekly bucketing. Falldown always uses
/// Due Date regardless of this selection.
/// </summary>
public enum MpsDateBasis
{
    DueDate,
    ReleaseDate
}

namespace Kst.Domain.Mps;

/// <summary>
/// Execution status derived only from distinct contributing Allocating/Frozen/Released states in a
/// bucket. Planned and ExplicitlyScheduled never create <see cref="Mixed"/> by themselves.
/// </summary>
public enum MpsExecutionStatus
{
    None,
    Allocating,
    Frozen,
    Released,
    Mixed
}

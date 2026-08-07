namespace Kst.Domain.Mps;

/// <summary>
/// Normalized work-order status for a qualifying MPS source fact. QAD's closed status ('C') is
/// excluded at the SQL source boundary and never reaches this type.
/// </summary>
public enum MpsWorkOrderState
{
    Allocating,
    Frozen,
    Released,
    Planned,
    ExplicitlyScheduled,

    /// <summary>Defensive normalization for an unexpected non-C raw status.</summary>
    Unknown
}

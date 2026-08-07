namespace Kst.Domain.Mps;

/// <summary>
/// Normalized MRP supply classification for a qualifying MPS source fact.
/// Maps from QAD <c>mrp_type</c> values SUPPLY / SUPPLYF / SUPPLYP.
/// </summary>
public enum MpsSupplyType
{
    Supply,
    SupplyF,
    SupplyP
}

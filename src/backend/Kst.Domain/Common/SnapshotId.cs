namespace Kst.Domain.Common;

/// <summary>
/// Unique identifier for a data snapshot loaded into memory.
/// </summary>
public readonly record struct SnapshotId(Guid Value)
{
    public static SnapshotId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

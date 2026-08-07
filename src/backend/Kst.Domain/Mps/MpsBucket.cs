namespace Kst.Domain.Mps;

/// <summary>
/// Normalized business state for one parent-part schedule cell after aggregation. <see cref="WeekLabel"/>
/// is the Monday anchor for weekly buckets and is null for the Falldown bucket.
/// </summary>
public sealed record MpsBucket(
    MpsBucketKind Kind,
    DateOnly? WeekLabel,
    decimal Quantity,
    MpsExecutionStatus ExecutionStatus,
    bool ContainsPlannedWork,
    bool ContainsExplicitlyScheduledWork,
    IReadOnlyList<MpsWorkOrderRef> WorkOrders
);

namespace Kst.Domain.Shortages;

/// <summary>Identifies the Stage 7R context used to order a WO in the Stage 9 allocation pass.</summary>
public enum PlanningBucketContext
{
    Falldown,
    ForwardDue,
    ForwardRelease
}

namespace Kst.Domain.Shortages;

/// <summary>Explains how residual requirement was evaluated after authoritative hard-allocation coverage.</summary>
public enum AllocationMode
{
    HardAllocatedCommitted,
    CommittedSequential,
    AdvisorySharedPool
}

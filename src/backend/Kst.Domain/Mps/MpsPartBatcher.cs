namespace Kst.Domain.Mps;

/// <summary>
/// Chunks a resolved parent-part list into bounded batches so QAD adapters never issue one query
/// per part nor build an unbounded parameterized statement. Pure/testable; no SQL concepts.
/// </summary>
public static class MpsPartBatcher
{
    public const int DefaultMaxBatchSize = 500;

    public static IReadOnlyList<IReadOnlyList<string>> Batch(
        IReadOnlyList<string> parts,
        int maxBatchSize = DefaultMaxBatchSize)
    {
        if (maxBatchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxBatchSize), "Batch size must be positive.");

        var batches = new List<IReadOnlyList<string>>();
        for (var offset = 0; offset < parts.Count; offset += maxBatchSize)
        {
            batches.Add(parts.Skip(offset).Take(maxBatchSize).ToList());
        }

        return batches;
    }
}

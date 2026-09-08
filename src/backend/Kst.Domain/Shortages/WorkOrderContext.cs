namespace Kst.Domain.Shortages;

/// <summary>Stage 9 work-order facts required to explain and order component material analysis.</summary>
public sealed record WorkOrderContext(
    string WoId,
    string BuildPart,
    string Status,
    string? WoType,
    DateOnly? DueDate,
    DateOnly? ReleaseDate,
    decimal MaterialBuildQuantity,
    PlanningBucketContext PlanningBucketContext);

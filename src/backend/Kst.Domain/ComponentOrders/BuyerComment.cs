namespace Kst.Domain.ComponentOrders;

/// <summary>The effective comment only. RecordedAt is source wall-clock time (Unspecified), not UTC.</summary>
public sealed record BuyerComment(string? Text, DateTime? RecordedAt = null, string? Author = null);

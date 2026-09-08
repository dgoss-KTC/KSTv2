namespace Kst.Domain.Shortages;

/// <summary>Next-PO/KSS context. It informs the scheduler but never reduces the current shortage.</summary>
public sealed record IncomingContext(
    bool IsKss,
    string? PoState,
    string? PoNumber,
    DateOnly? PoDueDate,
    decimal? PoOpenQuantity,
    bool? PoConfirmed,
    string? TrackingInfo);

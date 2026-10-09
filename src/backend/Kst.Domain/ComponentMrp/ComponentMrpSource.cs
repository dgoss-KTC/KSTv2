using Kst.Domain.LongTermShortages;

namespace Kst.Domain.ComponentMrp;

public sealed record ComponentMrpMetadata(string? Ios, int? TimeFence, int? InspectionLeadTime,
    decimal? MinimumOrder, decimal? OrderMultiple, decimal? Qctc);
public sealed record ComponentMrpSource(LongTermShortageInput Input, ComponentMrpMetadata Metadata,
    DateTimeOffset AcquiredAtUtc);

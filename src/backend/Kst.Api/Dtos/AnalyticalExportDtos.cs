namespace Kst.Api.Dtos;

public sealed record AnalyticalCellDto(string? Value, string Kind, string? Note);
public sealed record AnalyticalSheetDto(string Name, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<AnalyticalCellDto>> Rows, int FrozenColumns);
public sealed record AnalyticalMetadataDto(string Label, string Value);
public sealed record AnalyticalExportRequestDto(
    string Kind, Guid WorkspaceId, Guid MpsSnapshotId, string Site, DateOnly BusinessDate,
    string? Parent, string? DateBasis, int HorizonWeeks,
    DateTimeOffset? BomLoadedAtUtc, DateOnly? BomEffectiveDate, bool IsStale, string? Warning,
    string? ComponentFilter, string? DescriptionFilter, string? PmFilter, string? PhantomFilter,
    Guid? WorkbookSnapshotId, IReadOnlyList<AnalyticalMetadataDto> Metadata,
    IReadOnlyList<string> FiscalLabels, IReadOnlyList<AnalyticalSheetDto> Sheets,
    ExportOpenOrdersReportRequestDto? OpenOrders = null);
public sealed record AnalyticalExportPreparedDto(string PreparationId, string FileName, DateTimeOffset ExpiresAtUtc);

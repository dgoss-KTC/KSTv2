namespace Kst.Domain.Exports;

/// <summary>Literal presentation values, never spreadsheet expressions or executable formulas.</summary>
public sealed record AnalyticalCell(string? Value, string Kind = "text", string? Note = null);
public sealed record AnalyticalSheet(string Name, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<AnalyticalCell>> Rows, int FrozenColumns = 1);
public sealed record AnalyticalWorkbookDocument(string ReportType, IReadOnlyList<KeyValuePair<string, string>> Metadata, IReadOnlyList<AnalyticalSheet> Sheets);

using Kst.Exports.Contracts;

namespace Kst.Exports;

/// <summary>
/// Placeholder export service. No production functionality implemented yet.
/// </summary>
public sealed class PlaceholderExportService : IExportService
{
    public string GetDiagnosticStatus() => "Export service boundary established. No exports implemented yet.";
}

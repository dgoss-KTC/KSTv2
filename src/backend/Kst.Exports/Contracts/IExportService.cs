namespace Kst.Exports.Contracts;

/// <summary>
/// Marker interface for the export service boundary.
/// No production export methods implemented in this phase.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Returns diagnostic information confirming the export service is wired up.
    /// </summary>
    string GetDiagnosticStatus();
}

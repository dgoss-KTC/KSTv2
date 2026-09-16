using Kst.Domain.LongTermShortages;
namespace Kst.Exports.Contracts;

/// <summary>
/// Controlled workbook generation from an already loaded Stage 11-A projection.
/// </summary>
public interface IExportService
{
    byte[] CreateLongTermShortagesWorkbook(string workspaceName, DateOnly refreshDate, IReadOnlyList<LongTermShortageRow> rows);
}

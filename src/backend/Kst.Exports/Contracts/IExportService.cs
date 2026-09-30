using Kst.Domain.LongTermShortages;
using Kst.Domain.OpenOrders;
namespace Kst.Exports.Contracts;

/// <summary>
/// Controlled workbook generation from an already loaded Stage 11-A projection.
/// </summary>
public interface IExportService
{
    byte[] CreateLongTermShortagesWorkbook(string workspaceName, DateOnly refreshDate, IReadOnlyList<LongTermShortageRow> rows, DateTimeOffset? acquiredAtUtc = null, string? consistencyMode = null);
    byte[] CreateOpenOrdersWorkbook(IReadOnlyList<OpenOrderLine> rows, IReadOnlyList<string> columns, DateTimeOffset acquiredAtUtc, bool isStale);
}

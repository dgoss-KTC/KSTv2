using Kst.Application.ComponentOrders;
using Kst.Integrations.Shortages.Options;
using Microsoft.Data.SqlClient;

namespace Kst.Integrations.Shortages.ComponentOrders;

/// <summary>
/// Parameterized read-only Shortages enrichment for Component Orders. Missing source records are
/// represented by absent result entries; connection and source failures propagate to the caller.
/// </summary>
public sealed class ShortagesComponentOrderEnrichmentReader : IComponentOrderEnrichmentReader
{
    private const int BatchSize = 1000;
    private readonly ShortagesSecretFileLoader _secretLoader;

    public ShortagesComponentOrderEnrichmentReader(ShortagesSecretFileLoader? secretLoader = null) =>
        _secretLoader = secretLoader ?? new ShortagesSecretFileLoader();

    public async Task<ComponentOrderEnrichmentResult> ReadAsync(
        ComponentOrderEnrichmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var components = DistinctExact(request.ComponentIdentifiers);
        var suppliers = DistinctExact(request.SupplierIdentifiers);
        if (components.Count == 0 && suppliers.Count == 0)
            return ComponentOrderEnrichmentResult.Empty;

        if (!_secretLoader.TryLoad(out var settings) || settings is null)
            throw new InvalidOperationException("Shortages connection is not configured.");

        await using var connection = new SqlConnection(ShortagesConnectionStringFactory.Build(settings));
        await connection.OpenAsync(cancellationToken);

        var comments = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var batch in Batch(components))
        {
            await using var command = BuildCommentCommand(connection, request.Site, batch);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                comments[reader.GetString(0)] = NormalizeComment(reader.IsDBNull(1) ? null : reader.GetString(1));
        }

        var risks = new Dictionary<string, ComponentOrderSupplierRisk>(StringComparer.Ordinal);
        foreach (var batch in Batch(suppliers))
        {
            await using var command = BuildSupplierCommand(connection, batch);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                risks[reader.GetString(0)] = MapRisk(reader.GetBoolean(1), reader.GetBoolean(2));
        }

        return new ComponentOrderEnrichmentResult(comments, risks);
    }

    public static ComponentOrderSupplierRisk MapRisk(bool isCreditHold, bool isCia) => new(isCreditHold, isCia);

    public static string? NormalizeComment(string? comment) => comment;

    public static SqlCommand BuildSupplierCommand(SqlConnection connection, IReadOnlyList<string> supplierIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var command = connection.CreateCommand();
        command.CommandText = $"""
            WITH Scope (SupplierIdentifier) AS
            (
                SELECT SupplierIdentifier FROM (VALUES {BuildValues(command, supplierIdentifiers, "Supplier")}) AS scope (SupplierIdentifier)
            ),
            Ranked AS
            (
                SELECT ps.[Supplier Name] AS SupplierIdentifier, ps.RCHI AS IsCreditHold, ps.CIA AS IsCia,
                       ROW_NUMBER() OVER (PARTITION BY ps.[Supplier Name] ORDER BY ps.[Date] DESC, ps.ID DESC) AS RowNumber
                FROM dbo.PreferredSuppliers AS ps
                INNER JOIN Scope AS scope ON scope.SupplierIdentifier = ps.[Supplier Name]
            )
            SELECT SupplierIdentifier, IsCreditHold, IsCia
            FROM Ranked
            WHERE RowNumber = 1;
            """;
        return command;
    }

    public static SqlCommand BuildCommentCommand(SqlConnection connection, string site, IReadOnlyList<string> componentIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var command = connection.CreateCommand();
        command.Parameters.Add(new SqlParameter("@Site", System.Data.SqlDbType.VarChar, 2) { Value = site });
        command.CommandText = $"""
            WITH Scope (ComponentIdentifier) AS
            (
                SELECT ComponentIdentifier FROM (VALUES {BuildValues(command, componentIdentifiers, "Component")}) AS scope (ComponentIdentifier)
            ),
            Ranked AS
            (
                SELECT sm.Component AS ComponentIdentifier, sm.[Current Comments] AS CurrentComments,
                       ROW_NUMBER() OVER (PARTITION BY sm.Component ORDER BY sm.[Modification Date] DESC, sm.[Added Record Date] DESC, sm.id DESC) AS RowNumber
                FROM dbo.ShortageMaster AS sm
                INNER JOIN Scope AS scope ON scope.ComponentIdentifier = sm.Component
                WHERE sm.Site = @Site
                  AND sm.isRemoved = 0
            )
            SELECT ComponentIdentifier, CurrentComments
            FROM Ranked
            WHERE RowNumber = 1;
            """;
        return command;
    }

    private static string BuildValues(SqlCommand command, IReadOnlyList<string> values, string prefix)
    {
        if (values.Count == 0)
            throw new ArgumentException("A query batch must contain at least one identifier.", nameof(values));

        var parameterNames = new string[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            var name = $"@{prefix}{i}";
            command.Parameters.Add(new SqlParameter(name, System.Data.SqlDbType.NVarChar, 80) { Value = values[i] });
            parameterNames[i] = $"({name})";
        }
        return string.Join(", ", parameterNames);
    }

    private static List<string> DistinctExact(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
            if (value is not null && seen.Add(value)) result.Add(value);
        return result;
    }

    private static IEnumerable<IReadOnlyList<string>> Batch(IReadOnlyList<string> values)
    {
        for (var offset = 0; offset < values.Count; offset += BatchSize)
            yield return values.Skip(offset).Take(Math.Min(BatchSize, values.Count - offset)).ToArray();
    }
}
